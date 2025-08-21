using EAM.WebServices;
using HGT.EAM.WebServices.Infrastructure.Architecture.Query;
using static HGT.EAM.WebServices.Infrastructure.Architecture.Enums.GridEnums;
using static HGT.EAM.WebServices.Infrastructure.Architecture.Enums.GriTypeEnums;

namespace HGT.EAM.WebServices.Application.Queries;

public class GridDataOnlyGetQuery : IQuery<MP0116_GetGridDataOnly_001_Result>
{
    public required string Organization { get; set; } = string.Empty;

    public required string Username { get; set; } = string.Empty;

    public required string Password { get; set; } = string.Empty;

    public required string GridName { get; set; } = string.Empty;

    public required string FunctionName { get; set; } = string.Empty;

    public required int DataspyId { get; set; }

    public required int GridId { get; set; }

    public required int NumberOfRowsFirstReturned { get; set; }

    public required HGTGridTypeEnum GridTypeHGT { get; set; }

    public required HGTGridEnum GridHGT { get; set; }
}
