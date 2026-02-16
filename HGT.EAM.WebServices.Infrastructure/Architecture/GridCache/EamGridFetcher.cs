using EAM.WebServices;
using HGT.EAM.WebServices.Conector.Architecture.Extensions;
using HGT.EAM.WebServices.Conector.Architecture.Interfaces;
using HGT.EAM.WebServices.Conector.Architecture.Models;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

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
                        "Retry attempt {AttemptNumber} after {Delay}ms due to: {Exception}",
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
        int gridId,
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
            // Paso 1: Obtener metadata (header) con retry
            var headRequest = GetGridDataOnlyRequestExtensions.GetObject(organization, username, password ?? string.Empty, gridId, gridName, functionName, dataspyId, dateRanges, filterField, 0, pageSize);
            
            var (totalRows, fieldsEam) = await _retryPipeline.ExecuteAsync(async ct => 
                await gridService.GetHeadGridAsync(headRequest), cancellationToken);
            
            var fields = _mapper.Map<List<Field>>(fieldsEam);

            _logger.LogInformation(
                "Starting grid fetch for {GridName}: Total rows = {TotalRows}, PageSize = {PageSize}",
                gridName, totalRows, pageSize);

            // Paso 2: Iniciar sesión de caché
            await _cache.BeginCacheSessionAsync(cacheKey, totalRows, fields, cancellationToken);

            // Paso 3: Loop para obtener TODOS los datos con logging de progreso
            var cursorPosition = 1;
            var totalFetched = 0;
            var batchNumber = 0;

            while (true)
            {
                batchNumber++;
                
                // Fetch batch con retry policy
                var dataRequest = GetGridDataOnlyRequestExtensions.GetObject(organization, username, password ?? string.Empty, gridId, gridName, functionName, dataspyId, dateRanges, filterField, cursorPosition, pageSize);
                
                var response = await _retryPipeline.ExecuteAsync(async ct => 
                    await gridService.GetGridRowsAsync(dataRequest), cancellationToken);
                
                var rows = response.GRID.DATA != null
                    ? response.GRID.DATA.Items.ConvertToType<List<DATAROW>>().GetDTORows(fields)
                    : [];

                if (rows.Count > 0)
                {
                    await _cache.AppendCacheRowsAsync(cacheKey, rows, cursorPosition - 1, cancellationToken);
                    totalFetched += rows.Count;

                    // Logging de progreso
                    _logger.LogInformation(
                        "Grid {GridName}: Fetched batch {BatchNumber} -> {Current}/{Total} records ({Percentage:F1}%)",
                        gridName, batchNumber, totalFetched, totalRows, 
                        (totalFetched / (double)totalRows) * 100);
                }

                if (rows.Count < pageSize)
                    break;

                cursorPosition += rows.Count;
            }

            // Paso 4: Validar integridad de datos
            var cachedCount = await _cache.GetCachedRowCountAsync(cacheKey, cancellationToken);
            
            if (cachedCount != totalRows)
            {
                _logger.LogError(
                    "Data integrity check FAILED for grid {GridName}: Expected {Expected} rows, but cached {Actual} rows",
                    gridName, totalRows, cachedCount);
                
                // Rollback: eliminar datos parciales
                await _cache.RollbackCacheSessionAsync(cacheKey, cancellationToken);
                
                throw new InvalidOperationException(
                    $"Data integrity check failed: Expected {totalRows} rows, got {cachedCount} rows. Cache has been rolled back.");
            }

            _logger.LogInformation(
                "Grid {GridName}: Fetch completed successfully. {TotalRows} rows cached.",
                gridName, totalRows);

            return (totalRows, fields);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error fetching grid {GridName}. Rolling back cache session for key {CacheKey}",
                gridName, cacheKey);
            
            // Rollback en caso de cualquier error
            await _cache.RollbackCacheSessionAsync(cacheKey, cancellationToken);
            throw;
        }
    }
}
