using EAM.WebServices;
using HGT.EAM.WebServices.Conector.Architecture.Extensions;
using HGT.EAM.WebServices.Conector.Architecture.Interfaces;
using HGT.EAM.WebServices.Infrastructure.Architecture.Query;

namespace HGT.EAM.WebServices.Application.Queries;

public class GridDataOnlyGetQueryHandler : IQueryHandler<GridDataOnlyGetQuery, MP0116_GetGridDataOnly_001_Result>
{
    private readonly IEAMGridService _gridEAMService;

    public GridDataOnlyGetQueryHandler(IEAMGridService gridEAMService)
    {
        _gridEAMService = gridEAMService;
    }

    public async ValueTask<MP0116_GetGridDataOnly_001_Result> Handle(GridDataOnlyGetQuery command, CancellationToken cancellationToken)
    {
        var request = GetGridDataOnlyRequestExtensions.GetObject(command.Organization, command.Username, command.Password, command.GridId, command.GridName, command.FunctionName, command.DataspyId, 0, command.NumberOfRowsFirstReturned);
        var response = await _gridEAMService.GetGridInfoAsync(request);
        return response.MP0116_GetGridDataOnly_001_Result;
    }
}
