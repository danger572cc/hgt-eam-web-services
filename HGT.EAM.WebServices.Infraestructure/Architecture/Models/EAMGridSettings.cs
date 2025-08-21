using static HGT.EAM.WebServices.Infrastructure.Architecture.Enums.GridEnums;
using static HGT.EAM.WebServices.Infrastructure.Architecture.Enums.GriTypeEnums;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.Models;

public class EAMGridSettings
{
    public string GridName { get; set; } = string.Empty;
    public string UserFunction { get; set; } = string.Empty;
    public int GridId { get; set; }
    public DataSpyIds DataSpyIds { get; set; } = new DataSpyIds();
    public int NumberRecordsFirstReturned { get; set; }
    public int CursorPosition { get; set; }
    public HGTGridTypeEnum HGTGridType { get; set; }
    public HGTGridEnum HGTGridName { get; set; }
}

public class DataSpyIds
{
    public int Day { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}
