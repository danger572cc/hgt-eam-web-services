using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.Enums;

public static class ApiFilterEnums
{
    [Flags, JsonConverter(typeof(JsonStringEnumConverter<ApiRequestEnum>))]
    public enum ApiRequestEnum
    {
        Day = 1,
        Month = 2,
        Year = 3,
        Custom = 4
    }
}
