using EAM.WebServices;
using HGT.EAM.WebServices.Conector.Architecture.Extensions;
using HGT.EAM.WebServices.Conector.Architecture.Interfaces;
using HGT.EAM.WebServices.Conector.Architecture.Models;
using HGT.EAM.WebServices.Infrastructure.Architecture.Query;
using MapsterMapper;

namespace HGT.EAM.WebServices.Application.Queries;

public class GridDataOnlyGetQueryHandler : IQueryHandler<GridDataOnlyGetQuery, ResultDataGridModel>
{
    private readonly IGridCacheService _cache;
    private readonly IMapper _mapper;
    private readonly IServiceScopeFactory _scopeFactory;

    public GridDataOnlyGetQueryHandler(
        IGridCacheService cache,
        IMapper mapper,
        IServiceScopeFactory scopeFactory)
    {
        _cache = cache;
        _scopeFactory = scopeFactory;
        _mapper = mapper;
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

        var cached = await _cache.GetPageAsync(cacheKey, page, pageSize, cancellationToken);
        if (cached != null)
            return cached;

        var dateRanges = new List<DateTime>();
        var filterField = string.Empty;
        if (command.StartDate != null && command.EndDate != null)
        {
            dateRanges.Add(command.StartDate.GetValueOrDefault());
            dateRanges.Add(command.EndDate.GetValueOrDefault());
            filterField = command.FilterField;
        }

        using var scope = _scopeFactory.CreateScope();
        var gridService = scope.ServiceProvider.GetRequiredService<IEAMGridService>();

        var headRequest = GetGridDataOnlyRequestExtensions.GetObject(command.Organization, command.Username, command.Password, command.GridId, command.GridName, command.FunctionName, command.DataspyId, dateRanges, filterField, 0, pageSize);
        var (totalRows, fieldsEam) = await gridService.GetHeadGridAsync(headRequest);
        var fields = _mapper.Map<List<Field>>(fieldsEam);

        var allRows = new List<Dictionary<string, object>>(totalRows);
        var cursorPosition = 1;

        while (true)
        {
            var dataRequest = GetGridDataOnlyRequestExtensions.GetObject(command.Organization, command.Username, command.Password, command.GridId, command.GridName, command.FunctionName, command.DataspyId, dateRanges, filterField, cursorPosition, pageSize);
            var response = await gridService.GetGridRowsAsync(dataRequest);
            var rows = response.GRID.DATA != null
                ? response.GRID.DATA.Items.ConvertToType<List<DATAROW>>().GetDTORows(fields)
                : [];
            allRows.AddRange(rows);
            if (rows.Count < pageSize)
                break;
            cursorPosition += rows.Count;
        }

        await _cache.SaveFullGridAsync(cacheKey, totalRows, fields, allRows, cancellationToken);

        var fromCache = await _cache.GetPageAsync(cacheKey, page, pageSize, cancellationToken);
        if (fromCache != null)
            return fromCache;

        var skip = (page - 1) * pageSize;
        var pageRows = allRows.Skip(skip).Take(pageSize).ToList();
        var totalPages = (int)Math.Ceiling((double)totalRows / pageSize);
        return new ResultDataGridModel
        {
            TotalRecords = totalRows,
            TotalPages = totalPages,
            CurrentPage = page,
            TotalRecordsReturned = pageRows.Count,
            DataRecord = new DataRecord { Fields = fields, Rows = pageRows }
        };
    }
}
