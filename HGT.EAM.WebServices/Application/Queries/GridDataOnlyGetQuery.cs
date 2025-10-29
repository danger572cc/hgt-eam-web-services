using HGT.EAM.WebServices.Conector.Architecture.Models;
using HGT.EAM.WebServices.Infrastructure.Architecture.Enums;
using HGT.EAM.WebServices.Infrastructure.Architecture.Models;
using HGT.EAM.WebServices.Infrastructure.Architecture.Query;
using System.Security.Claims;
using static HGT.EAM.WebServices.Infrastructure.Architecture.Enums.ApiFilterEnums;
using static HGT.EAM.WebServices.Infrastructure.Architecture.Enums.GridEnums;
using static HGT.EAM.WebServices.Infrastructure.Architecture.Enums.GriTypeEnums;

namespace HGT.EAM.WebServices.Application.Queries;

public class GridDataOnlyGetQuery : IQuery<ResultDataGridModel>
{
    public GridDataOnlyGetQuery(
        ClaimsPrincipal userInfo, 
        ApiRequestEnum typeFilter, 
        EAMGridSettings gridConfiguration,
        HGTGridEnum gridHGT,
        HGTGridTypeEnum gridTypeHGT,
        int page,
        int? pageSize = null,
        int? month = null, 
        int? year = null)
    {
        ArgumentNullException.ThrowIfNull(userInfo);
        ArgumentNullException.ThrowIfNull(gridConfiguration);

        Username = string.Empty + userInfo?.Identity?.Name;
        Password = userInfo?.Claims.FirstOrDefault(i => i.Type == "Password")?.Value;
        Organization = userInfo?.Claims.FirstOrDefault(i => i.Type == "Organization")?.Value;
        FunctionName = gridConfiguration.UserFunction;
        GridName = gridConfiguration.GridName;
        GridId = gridConfiguration.GridId;
        Page = page;
        NumberOfRowsFirstReturned = !pageSize.HasValue ? gridConfiguration.NumberRecordsFirstReturned : pageSize.GetValueOrDefault();
        DataspyId = typeFilter switch
        {
            ApiRequestEnum.PreviousDay => gridConfiguration.DataSpyIds.PreviousDay,
            ApiRequestEnum.PreviousMonth => gridConfiguration.DataSpyIds.PreviousMonth,
            ApiRequestEnum.CurrentMonth => gridConfiguration.DataSpyIds.CurrentMonth,
            ApiRequestEnum.LastYear => gridConfiguration.DataSpyIds.LastYear,
            ApiRequestEnum.FullMonthByYear => gridConfiguration.DataSpyIds.AllRecords,
            ApiRequestEnum.Custom => gridConfiguration.DataSpyIds.Custom,
            ApiRequestEnum.AllRecords => gridConfiguration.DataSpyIds.AllRecords,
            _ => throw new InvalidOperationException("Invalid filter, accepted values ​​are: 1 = previous day, 2 = previous month, 3 = current month, 4 = Previous year, 5 = Specific month and year."),
        };
        GridHGT = gridHGT;
        GridTypeHGT = gridTypeHGT;
        if (month != null && year != null)
        {
            FilterField = gridConfiguration.FilterField;
            var initDate = new DateTime(year.GetValueOrDefault(), month.GetValueOrDefault(), 1);
            int lastDay = DateTime.DaysInMonth(initDate.Year, initDate.Month);
            StartDate = initDate;
            EndDate = new DateTime(year.GetValueOrDefault(), month.GetValueOrDefault(), lastDay, 23, 59, 0);
        }
    }

    public string Organization { get; private set; }

    public string Username { get; private set;}

    public string Password { get; private set; }

    public string GridName { get; private set; }

    public string FunctionName { get; private set; }

    public int DataspyId { get; private set; }

    public int GridId { get; private set; }

    public int Page { get; private set; }

    public int NumberOfRowsFirstReturned { get; private set; }

    public string FilterField { get; private set; }

    public DateTime? StartDate { get; private set; }

    public DateTime? EndDate { get; private set; }

    public HGTGridTypeEnum GridTypeHGT { get; private set; }

    public HGTGridEnum GridHGT { get; private set; }
}
