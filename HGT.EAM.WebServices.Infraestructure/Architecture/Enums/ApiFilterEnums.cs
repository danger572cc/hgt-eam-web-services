using System.Text.Json.Serialization;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.Enums;

public static class ApiFilterEnums
{
    [Flags, JsonConverter(typeof(JsonStringEnumConverter<ApiRequestEnum>))]
    public enum ApiRequestEnum
    {
        PreviousDay = 1,
        PreviousMonth = 2,
        CurrentMonth = 3,
        LastYear = 4,
        FullMonthByYear = 5,
        Custom = 6,
        AllRecords = 7
    }
}