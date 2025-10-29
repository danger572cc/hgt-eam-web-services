using EAM.WebServices;

namespace HGT.EAM.WebServices.Conector.Architecture.Extensions;

public static class GetGridDataOnlyRequestExtensions
{
    public static GetGridDataOnlyRequestMsg GetObject(
        string organization, 
        string username, 
        string password, 
        int gridId, 
        string gridName, 
        string functionName,
        int dataSpyId,
        List<DateTime> dateRanges,
        string fieldFilter = "",
        int cursorPosition = 0,
        int numberOfRows = 2000) 
    {
        var authentication = new UsernameToken()
        {
            Username = new Username { Value = username },
            Password = new Password { Value = password }
        };

        var security = new Security()
        {
            Any = [authentication.SerializeToXmlElement()]
        };

        var request = new GetGridDataOnlyRequestMsg() 
        {
            Organization = organization,
            Security = security,
            MP0116_GetGridDataOnly_001 = new MP0116_GetGridDataOnly_001() 
            {
                FUNCTION_REQUEST_INFO = new FUNCTION_REQUEST_INFO() 
                {
                    DATASPY = new DATASPY { DATASPY_ID = dataSpyId.ToString() },
                    GRID_TYPE = new GRID_TYPE { TYPE = GRID_TYPE_type.LIST, TYPESpecified = true },
                    GRID = new GRID { 
                        GRID_ID = gridId.ToString(), 
                        GRID_NAME = gridName, 
                        USER_FUNCTION_NAME = functionName,
                        CURSOR_POSITION = cursorPosition.ToString(),
                        NUMBER_OF_ROWS_FIRST_RETURNED = numberOfRows.ToString(),
                        LOCALIZE_RESULT = "true",
                        RESULT_IN_SAXORDER = "true"
                    },
                    REQUEST_TYPE = FUNCTION_REQUEST_TYPE.LISTDATA_ONLYSTORED,
                    REQUEST_TYPESpecified = true,
                }
            }
        };
        //filtro por rango
        if (!string.IsNullOrEmpty(fieldFilter) && dateRanges?.Count > 0) 
        {
            var customFilters = new List<MULTIADDON_FILTERSMADDON_FILTER>
            {
                new MULTIADDON_FILTERSMADDON_FILTER
                {
                    ALIAS_NAME = fieldFilter,
                    OPERATOR = OPERATOR_TYPE.Item4,
                    OPERATORSpecified = true,
                    VALUE = dateRanges[0].ToString("MM/dd/yyyy HH:mm"),
                    JOINERSpecified = true,
                    JOINER = AND_OR.AND,
                    SEQNUM = "1"
                },
                new MULTIADDON_FILTERSMADDON_FILTER
                {
                    ALIAS_NAME = fieldFilter,
                    OPERATOR = OPERATOR_TYPE.Item7,
                    OPERATORSpecified = true,
                    VALUE = dateRanges[1].ToString("MM/dd/yyyy HH:mm"),
                    JOINERSpecified = false,
                    SEQNUM = "2"
                }
            };
            request.MP0116_GetGridDataOnly_001.FUNCTION_REQUEST_INFO.MULTIADDON_FILTERS = [.. customFilters];
            request.MP0116_GetGridDataOnly_001.FUNCTION_REQUEST_INFO.ADDON_SORT = new ADDON_SORT 
            {
                ALIAS_NAME = fieldFilter,
                TYPE = SORT_TYPE.ASC,
                TYPESpecified = true
            };
        }
        return request;
    }
}
