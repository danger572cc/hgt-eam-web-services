using HGT.EAM.WebServices.Conector.Architecture.Extensions;
using HGT.EAM.WebServices.Conector.Architecture.Interfaces;
using HGT.EAM.WebServices.Conector.Architecture.Models;
using HGT.EAM.WebServices.Infrastructure.Architecture.Interfaces;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using DATAROW = EAM.WebServices.DATAROW;
using EnumsGrid = EAM.WebServices.METADATAMORERECORDPRESENT;
using EnumsGridCache = EAM.WebServices.GridCache.METADATAMORERECORDPRESENT;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.GridCache;

public class EamGridFetcher : IEamGridFetcher
{
    private readonly IGridCacheService _cache;
    private readonly IMapper _mapper;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EamGridFetcher> _logger;
    private readonly ResiliencePipeline _retryPipeline;

    // Ajustar según el caso: 2000, 5000, o 10000
    private const int BATCH_SIZE_FOR_CACHE = 5000;

    // Delay después del primer MP0116 para dar tiempo a que la sesión se active
    private const int SESSION_ACTIVATION_DELAY_MS = 300;

    public EamGridFetcher(
        IGridCacheService cache,
        IMapper mapper,
        IServiceScopeFactory scopeFactory,
        ILogger<EamGridFetcher> logger)
    {
        _cache = cache;
        _mapper = mapper;
        _scopeFactory = scopeFactory;
        _logger = logger;

        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                // No reintentar errores semánticos del servidor EAM (FaultException)
                // ni cancelaciones: sólo errores transitorios de red/timeout.
                ShouldHandle = new PredicateBuilder()
                    .Handle<Exception>(ex =>
                        ex is not System.ServiceModel.FaultException &&
                        ex is not OperationCanceledException),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Reintento {AttemptNumber} después de {Delay}ms debido a: {Exception}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? "Error desconocido");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<(int TotalRows, List<Field> Fields)> FetchAndCacheAsync(
        string cacheKey,
        string username,
        string organization,
        string? password,
        long gridId,
        string gridName,
        string functionName,
        int dataspyId,
        DateTime? startDate,
        DateTime? endDate,
        string filterField,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var dateRanges = new List<DateTime>();
        if (startDate != null && endDate != null)
        {
            dateRanges.Add(startDate.GetValueOrDefault());
            dateRanges.Add(endDate.GetValueOrDefault());
        }

        using var scope = _scopeFactory.CreateScope();
        var gridService = scope.ServiceProvider.GetRequiredService<IEAMGridService>();

        try
        {
            var cursorPosition = 1;
            var totalFetched = 0;
            var batchNumber = 0;
            string? sessionId = null;
            string? moreRecordsPresent;
            List<Field>? fields = null;

            // Buffer temporal para acumular rows antes de guardar
            var bufferRows = new List<Dictionary<string, object>>();
            var bufferStartIndex = 0;

            var mainRequest = GetGridDataOnlyRequestExtensions.GetRequestObject(
                organization, username, password ?? string.Empty,
                gridId, gridName, functionName,
                dataspyId, dateRanges, filterField,
                cursorPosition, pageSize);

            do
            {
                batchNumber++;
                List<Dictionary<string, object>> rows;

                if (batchNumber == 1)
                {
                    // Primera llamada: obtener campos
                    var fieldsResponse = await _retryPipeline.ExecuteAsync(async ct =>
                        await gridService.GetHeadGridAsync(mainRequest), cancellationToken);

                    // Segunda llamada: MP0116 con SessionScenario="start" 
                    var (capturedSessionId, result) = await _retryPipeline.ExecuteAsync(async ct =>
                        await gridService.GetGridRowsAsync(mainRequest), cancellationToken);

                    sessionId = capturedSessionId;

                    // DELAY: Dar tiempo al servidor EAM para activar la sesión
                    // Esto evita el error "There is no active session" en el primer MP0117.
                    await Task.Delay(SESSION_ACTIVATION_DELAY_MS);

                    var fieldsEam = fieldsResponse ?? [];
                    fields = _mapper.Map<List<Field>>(fieldsEam);

                    rows = result.GRID.DATA != null
                        ? result.GRID.DATA.Items.ConvertToType<List<DATAROW>>().GetDTORows(fields)
                        : [];

                    _logger.LogInformation(
                        "Grilla {GridName}: Primer lote - {RowCount} registros obtenidos, SessionID = {SessionId}",
                        gridName, rows.Count, sessionId);

                    await _cache.BeginCacheSessionAsync(cacheKey, fields, gridId, gridName, cancellationToken);

                    moreRecordsPresent = result.GRID.METADATA?.MORERECORDPRESENT == EnumsGrid.Item ? "+" : "-";
                    cursorPosition = int.Parse(result.GRID.METADATA?.CURRENTCURSORPOSITION ?? "0") + 1;
                }
                else
                {
                    // LLAMADAS SUBSECUENTES: MP0117
                    var cacheRequest = GetGridDataOnlyRequestExtensions.GetCacheRequestObject(
                        username, password ?? string.Empty,
                        mainRequest,
                        sessionId!,
                        cursorPosition);

                    var cacheResult = await _retryPipeline.ExecuteAsync(async ct =>
                        await gridService.GetGridCacheRowsAsync(cacheRequest), cancellationToken);

                    rows = cacheResult.GRID.DATA != null
                        ? cacheResult.GRID.DATA.Items.ConvertToType<List<DATAROW>>().GetDTORows(fields!)
                        : [];

                    moreRecordsPresent = cacheResult.GRID.METADATA?.MORERECORDPRESENT == EnumsGridCache.Item ? "+" : "-";
                    cursorPosition = int.Parse(cacheResult.GRID.METADATA?.CURRENTCURSORPOSITION ?? "0") + 1;

                    _logger.LogInformation(
                        "Grilla {GridName}: Lote {BatchNumber} - MP0117, {RowCount} registros",
                        gridName, batchNumber, rows.Count);
                }

                if (rows.Count > 0)
                {
                    bufferRows.AddRange(rows);
                    totalFetched += rows.Count;

                    // Guardar en SQLite cada BATCH_SIZE_FOR_CACHE registros
                    if (bufferRows.Count >= BATCH_SIZE_FOR_CACHE)
                    {
                        _logger.LogInformation(
                            "Grilla {GridName}: Guardando batch de {BatchCount} registros en caché (total acumulado: {TotalFetched})...",
                            gridName, bufferRows.Count, totalFetched);

                        await _cache.AppendCacheRowsAsync(cacheKey, bufferRows, bufferStartIndex, cancellationToken);

                        bufferStartIndex += bufferRows.Count;
                        bufferRows.Clear(); // Liberar memoria
                    }
                }

            } while (moreRecordsPresent == "+");

            // Guardar el último batch (registros sobrantes < BATCH_SIZE_FOR_CACHE)
            if (bufferRows.Count > 0)
            {
                _logger.LogInformation(
                    "Grilla {GridName}: Guardando último batch de {BatchCount} registros en caché...",
                    gridName, bufferRows.Count);

                await _cache.AppendCacheRowsAsync(cacheKey, bufferRows, bufferStartIndex, cancellationToken);
                bufferRows.Clear();
            }

            await _cache.UpdateTotalCountAsync(cacheKey, totalFetched, cancellationToken);

            var cachedCount = await _cache.GetCachedRowCountAsync(cacheKey, cancellationToken);

            if (cachedCount != totalFetched)
            {
                _logger.LogError(
                    "Verificación de integridad FALLIDA para la grilla {GridName}: Se esperaban {Expected} registros, pero se cachearon {Actual}",
                    gridName, totalFetched, cachedCount);

                await _cache.RollbackCacheSessionAsync(cacheKey, cancellationToken);

                throw new InvalidOperationException(
                    $"Verificación de integridad fallida: Se esperaban {totalFetched} registros, se obtuvieron {cachedCount}. El caché ha sido revertido.");
            }

            _logger.LogInformation(
                "Grilla {GridName}: Carga completada exitosamente. {TotalRows} registros cacheados.",
                gridName, totalFetched);

            // MARCAR CACHÉ COMO COMPLETADO
            await _cache.CompleteCacheSessionAsync(cacheKey, cancellationToken);

            return (totalFetched, fields!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error capturando datos del grid {GridName}. Revirtiendo caché con clave {CacheKey}",
                gridName, cacheKey);
            await _cache.RollbackCacheSessionAsync(cacheKey, CancellationToken.None);
            throw;
        }
    }
}