using EAM.WebServices;
using HGT.EAM.WebServices.Conector.Architecture.Extensions;
using HGT.EAM.WebServices.Conector.Architecture.Interfaces;
using HGT.EAM.WebServices.Conector.Architecture.Models;
using HGT.EAM.WebServices.Infrastructure.Architecture.Query;
using MapsterMapper;

namespace HGT.EAM.WebServices.Application.Queries;

public class GridDataOnlyGetQueryHandler : IQueryHandler<GridDataOnlyGetQuery, ResultDataGridModel>
{
    private readonly IMapper _mapper;

    private readonly IServiceScopeFactory _scopeFactory;

    private List<FIELD> _fields;

    private int _totalRowsObtained;

    public GridDataOnlyGetQueryHandler(IMapper mapper, IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _mapper = mapper;
    }

    public async ValueTask<ResultDataGridModel> Handle(GridDataOnlyGetQuery command, CancellationToken cancellationToken)
    {
        var dateRanges = new List<DateTime>();
        string filterField = string.Empty;
        if (command.StartDate != null && command.EndDate != null)
        {
            dateRanges.Add(command.StartDate.GetValueOrDefault());
            dateRanges.Add(command.EndDate.GetValueOrDefault());
            filterField = command.FilterField;
        }
        int page = command.Page > 0 ? (command.NumberOfRowsFirstReturned * (command.Page - 1)) + 1 : 0;
        var request = GetGridDataOnlyRequestExtensions.GetObject(command.Organization, command.Username, command.Password, command.GridId, command.GridName, command.FunctionName, command.DataspyId, dateRanges, filterField, page, command.NumberOfRowsFirstReturned);

        MP0116_GetGridDataOnly_001_ResultGRIDRESULT? response = null;

        using (var scope = _scopeFactory.CreateScope())
        {
            var gridService = scope.ServiceProvider.GetRequiredService<IEAMGridService>();
            if (page == 1 || _fields == null) 
            {
                var headResponse = await gridService.GetHeadGridAsync(request);
                _totalRowsObtained = headResponse.Item1;
                _fields = headResponse.Item2;
            }
            response = await gridService.GetGridRowsAsync(request);
        }

        var fields = _mapper.Map<List<Field>>(_fields);
        var rows = response.GRID.DATA != null ? response.GRID.DATA.Items.ConvertToType<List<DATAROW>>().GetDTORows(fields) : [];
        var responseDTO = new ResultDataGridModel 
        {
            TotalRecords = _totalRowsObtained,
            TotalPages = (int)Math.Ceiling((double)_totalRowsObtained / command.NumberOfRowsFirstReturned),
            CurrentPage = command.Page,
            TotalRecordsReturned = rows.Count,
            DataRecord = new DataRecord 
            {
                Fields = fields,
                Rows = rows
            }
        };
        return responseDTO;
    }
}
