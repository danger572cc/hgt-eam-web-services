using HGT.EAM.WebServices.Conector.Architecture.Extensions;
using HGT.EAM.WebServices.Conector.Architecture.Interfaces;
using HGT.EAM.WebServices.Conector.Architecture.Models;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using DATAROW = EAM.WebServices.DATAROW;
using EnumsGridCache = EAM.WebServices.GridCache.METADATAMORERECORDPRESENT;
using EnumsGrid = EAM.WebServices.METADATAMORERECORDPRESENT;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.GridCache;

public class EamGridFetcher : IEamGridFetcher
{
    private readonly IGridCacheService _cache;
    private readonly IMapper _mapper;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EamGridFetcher> _logger;
    private readonly ResiliencePipeline _retryPipeline;

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

        // Configurar Polly retry policy con backoff exponencial
        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Reintento {AttemptNumber} después de {Delay}ms debido a: {Exception}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? "Unknown error");
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

            // Construir el request base MP0116 - se reutiliza como plantilla para MP0117
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
                    // Primera llamada: MP0116 obtener campos.
                    var fieldsResponse = await _retryPipeline.ExecuteAsync(async ct =>
                        await gridService.GetHeadGridAsync(mainRequest), cancellationToken);

                    // Segunda llamada: MP0116 con SessionScenario="start" 
                    var (capturedSessionId, result) = await _retryPipeline.ExecuteAsync(async ct =>
                        await gridService.GetGridRowsAsync(mainRequest), cancellationToken);
                    sessionId = capturedSessionId;

                    // Extraer fields y registros de la primera respuesta
                    var fieldsEam = fieldsResponse ?? [];
                    fields = _mapper.Map<List<Field>>(fieldsEam);

                    rows = result.GRID.DATA != null
                        ? result.GRID.DATA.Items.ConvertToType<List<DATAROW>>().GetDTORows(fields)
                        : [];

                    _logger.LogInformation(
                        "Grilla {GridName}: Primer lote -  cantidad de registros obtenidos = {RowFetched}, SessionID = {SessionId}",
                        gridName, rows.Count, sessionId);

                    await _cache.BeginCacheSessionAsync(cacheKey, fields, gridId, gridName, cancellationToken);
                    moreRecordsPresent = result.GRID.METADATA?.MORERECORDPRESENT == EnumsGrid.Item ? "+" : "-";
                    cursorPosition = int.Parse(result.GRID.METADATA?.CURRENTCURSORPOSITION ?? "0") + 1;
                }
                else
                {
                    // LLAMADAS SUBSECUENTES: MP0117 con SessionScenario="continue"
                    // GetCacheRequestObject reutiliza mainRequest como plantilla
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
                        "Grilla {GridName}: Lote {BatchNumber} - MP0117, Cursor = {Cursor}",
                        gridName, batchNumber, cursorPosition);
                }

                if (rows.Count > 0)
                {
                    await _cache.AppendCacheRowsAsync(cacheKey, rows, totalFetched, cancellationToken);
                    totalFetched += rows.Count;
                }

            } while (moreRecordsPresent == "+"); // METADATAMORERECORDPRESENT = '+'

            // Actualizo los registros totales.
            await _cache.UpdateTotalCountAsync(cacheKey, totalFetched, cancellationToken);

            // Validar integridad - usamos totalFetched (real) no totalRows (puede ser aproximado)
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

            return (totalFetched, fields!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error capturando datos del grid {GridName}. Rolling back la sesión de caché con clave {CacheKey}",
                gridName, cacheKey);
            
            // Rollback en caso de cualquier error
            await _cache.RollbackCacheSessionAsync(cacheKey, cancellationToken);
            throw;
        }
    }
}
