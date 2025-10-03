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

    public string FilterField { get; set; } = string.Empty;
}

public class DataSpyIds
{
    public int PreviousDay { get; set; }
    public int PreviousMonth { get; set; }
    public int CurrentMonth { get; set; }
    public int LastYear { get; set; }
    public int Custom { get; set; }
    public int AllRecords { get; set; }
}
