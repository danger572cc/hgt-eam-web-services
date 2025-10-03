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

    private readonly IEAMGridService _gridEAMService;

    public GridDataOnlyGetQueryHandler(IMapper mapper, IEAMGridService gridEAMService)
    {
        _gridEAMService = gridEAMService;
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
        var response = await _gridEAMService.GetGridInfoAsync(request);
        
        var fields = _mapper.Map<List<Field>>(response.Item2);
        var rows = response.Item3.GRID.DATA != null ? response.Item3.GRID.DATA.Items.ConvertToType<List<DATAROW>>().GetDTORows(fields) : [];
        var responseDTO = new ResultDataGridModel 
        {
            TotalRecords = response.Item1,
            TotalPages = (int)Math.Ceiling((double)response.Item1 / command.NumberOfRowsFirstReturned),
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
