using HGT.EAM.WebServices.Conector.Architecture.Interfaces;
using HGT.EAM.WebServices.Conector.Architecture.Models;
using HGT.EAM.WebServices.Infrastructure.Architecture.GridCache;
using HGT.EAM.WebServices.Infrastructure.Architecture.Query;

using Mediator;

namespace HGT.EAM.WebServices.Application.Queries;

public class GridDataOnlyGetQueryHandler : IRequestHandler<GridDataOnlyGetQuery, ResultDataGridModel>
{
    private readonly IGridCacheService _cache;
    private readonly IEamGridFetcher _fetcher;

    public GridDataOnlyGetQueryHandler(
        IGridCacheService cache,
        IEamGridFetcher fetcher)
    {
        _cache = cache;
        _fetcher = fetcher;
    }

    public async ValueTask<ResultDataGridModel> Handle(GridDataOnlyGetQuery command, CancellationToken cancellationToken)
    {
        var cacheKey = _cache.ComputeCacheKey(
            command.Username,
            command.Organization,
            command.GridId,
            command.GridName,
            command.FunctionName,
            command.DataspyId,
            command.StartDate,
            command.EndDate,
            command.FilterField);

        var page = command.Page > 0 ? command.Page : 1;
        var pageSize = command.NumberOfRowsFirstReturned;

        // 1. Try Cache
        var cached = await _cache.GetPageAsync(cacheKey, page, pageSize, cancellationToken);
        if (cached != null) 
        {
            return cached;
        }

        // 2. Fetch & Cache (Streamed)
        var (totalRows, fields) = await _fetcher.FetchAndCacheAsync(
            cacheKey,
            command.Username,
            command.Organization,
            command.Password,
            command.GridId,
            command.GridName,
            command.FunctionName,
            command.DataspyId,
            command.StartDate,
            command.EndDate,
            command.FilterField,
            pageSize,
            cancellationToken);

        // 3. Return from Cache (guaranteed to be there now)
        // Note: In the previous implementation we optimized by capturing the page rows in memory during fetch.
        // To strictly separate responsibilities, we can re-query the cache. 
        // SQLite is fast enough for this "Read-Your-Writes" pattern on a single page.
        // It keeps the handler logic pure: Check Cache -> Miss -> Fill Cache -> Read Cache.
        
        var freshResult = await _cache.GetPageAsync(cacheKey, page, pageSize, cancellationToken);
        if (freshResult != null) 
        {
            return freshResult;
        }

        // Fallback (should not happen if fetch worked)
        var totalPages = (int)Math.Ceiling((double)totalRows / pageSize);
        return new ResultDataGridModel
        {
            TotalRecords = totalRows,
            TotalPages = totalPages,
            CurrentPage = page,
            TotalRecordsReturned = 0,
            DataRecord = new DataRecord { Fields = fields, Rows = [] }
        };
    }
}
