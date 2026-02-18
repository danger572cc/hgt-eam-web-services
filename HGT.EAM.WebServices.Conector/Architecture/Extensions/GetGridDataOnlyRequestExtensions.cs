using EAM.WebServices;
using Grid = EAM.WebServices;
using GridCache = EAM.WebServices.GridCache;

namespace HGT.EAM.WebServices.Conector.Architecture.Extensions;

public static class GetGridDataOnlyRequestExtensions
{
    public static Grid.GetGridDataOnlyRequestMsg GetRequestObject(
        string organization, 
        string username, 
        string password, 
        long gridId,
        string gridName,
        string functionName,
        int dataSpyId,
        List<DateTime> dateRanges,
        string fieldFilter = "",
        int cursorPosition = 0,
        int numberOfRows = 2000)
    {
        var authentication = new Grid.UsernameToken()
        {
            Username = new Grid.Username { Value = username },
            Password = new Grid.Password { Value = password }
        };

        var security = new Grid.Security()
        {
            Any = [authentication.SerializeToXmlElement()]
        };

        var request = new Grid.GetGridDataOnlyRequestMsg
        {
            Organization = organization,
            Security = security,
            MP0116_GetGridDataOnly_001 = new Grid.MP0116_GetGridDataOnly_001()
            {
                FUNCTION_REQUEST_INFO = new Grid.FUNCTION_REQUEST_INFO()
                {
                    DATASPY = new Grid.DATASPY { DATASPY_ID = dataSpyId.ToString() },
                    GRID_TYPE = new Grid.GRID_TYPE { TYPE = Grid.GRID_TYPE_type.LIST, TYPESpecified = true },
                    GRID = new Grid.GRID
                    {
                        GRID_ID = gridId.ToString(),
                        GRID_NAME = gridName,
                        USER_FUNCTION_NAME = functionName,
                        CURSOR_POSITION = cursorPosition.ToString(),
                        NUMBER_OF_ROWS_FIRST_RETURNED = numberOfRows.ToString(),
                        LOCALIZE_RESULT = "true",
                        RESULT_IN_SAXORDER = "true"
                    },
                    REQUEST_TYPE = Grid.FUNCTION_REQUEST_TYPE.LISTDATA_ONLYSTORED,
                    REQUEST_TYPESpecified = true,
                }
            },
            //Se establece el escenario de la sesión para evitar problemas con el cache del grid
            SessionScenario = "start"
        };
        //filtro por rango
        if (!string.IsNullOrEmpty(fieldFilter) && dateRanges?.Count > 0) 
        {
            var customFilters = new List<Grid.MULTIADDON_FILTERSMADDON_FILTER>
            {
                new Grid.MULTIADDON_FILTERSMADDON_FILTER
                {
                    ALIAS_NAME = fieldFilter,
                    OPERATOR = Grid.OPERATOR_TYPE.Item4,
                    OPERATORSpecified = true,
                    VALUE = dateRanges[0].ToString("MM/dd/yyyy HH:mm"),
                    JOINERSpecified = true,
                    JOINER = Grid.AND_OR.AND,
                    SEQNUM = "1"
                },
                new Grid.MULTIADDON_FILTERSMADDON_FILTER
                {
                    ALIAS_NAME = fieldFilter,
                    OPERATOR = Grid.OPERATOR_TYPE.Item7,
                    OPERATORSpecified = true,
                    VALUE = dateRanges[1].ToString("MM/dd/yyyy HH:mm"),
                    JOINERSpecified = false,
                    SEQNUM = "2"
                }
            };
            request.MP0116_GetGridDataOnly_001.FUNCTION_REQUEST_INFO.MULTIADDON_FILTERS = [.. customFilters];
            request.MP0116_GetGridDataOnly_001.FUNCTION_REQUEST_INFO.ADDON_SORT = new Grid.ADDON_SORT 
            {
                ALIAS_NAME = fieldFilter,
                TYPE = Grid.SORT_TYPE.ASC,
                TYPESpecified = true
            };
        }
        return request;
    }

    public static GridCache.GetGridDataOnlyCacheRequestMsg GetCacheRequestObject(
        string username,
        string password,
        Grid.GetGridDataOnlyRequestMsg mainRequest,
        string sessionId,
        int cursorPosition)
    {
        var authentication = new UsernameToken()
        {
            Username = new Username { Value = username },
            Password = new Password { Value = password }
        };

        var security = new GridCache.Security()
        {
            Any = [authentication.SerializeToXmlElement()]
        };

        var request = new GridCache.GetGridDataOnlyCacheRequestMsg()
        {
            Organization = mainRequest.Organization,
            Security = security,
            SessionScenario = "continue",
            Session = new GridCache.SessionType
            {
                sessionId = sessionId
            },
            MP0117_GetGridDataOnlyCache_001 = new GridCache.MP0117_GetGridDataOnlyCache_001()
            {
                FUNCTION_REQUEST_INFO = mainRequest.MP0116_GetGridDataOnly_001.ToFormat0117(cursorPosition)
            }
        };
        //Se establece el escenario de la sesión para evitar problemas con el cache del grid
        request.SessionScenario = "continue";
        return request;
    }

    private static GridCache.FUNCTION_REQUEST_INFO ToFormat0117(this Grid.MP0116_GetGridDataOnly_001 resquest, int cursorPosition)
    {
        return new GridCache.FUNCTION_REQUEST_INFO()
        {
            DATASPY = new GridCache.DATASPY { DATASPY_ID = resquest.FUNCTION_REQUEST_INFO.DATASPY.DATASPY_ID },
            GRID_TYPE = new GridCache.GRID_TYPE { TYPE = GridCache.GRID_TYPE_type.LIST, TYPESpecified = true },
            GRID = new GridCache.GRID
            {
                GRID_ID = resquest.FUNCTION_REQUEST_INFO.GRID.GRID_ID,
                GRID_NAME = resquest.FUNCTION_REQUEST_INFO.GRID.GRID_NAME,
                USER_FUNCTION_NAME = resquest.FUNCTION_REQUEST_INFO.GRID.USER_FUNCTION_NAME,
                CURSOR_POSITION = cursorPosition.ToString(),
                NUMBER_OF_ROWS_FIRST_RETURNED = resquest.FUNCTION_REQUEST_INFO.GRID.NUMBER_OF_ROWS_FIRST_RETURNED,
                LOCALIZE_RESULT = resquest.FUNCTION_REQUEST_INFO.GRID.LOCALIZE_RESULT,
                RESULT_IN_SAXORDER = resquest.FUNCTION_REQUEST_INFO.GRID.RESULT_IN_SAXORDER
            },
            REQUEST_TYPE = GridCache.FUNCTION_REQUEST_TYPE.LISTDATA_ONLYCACHE,
            REQUEST_TYPESpecified = true,
        };
    }
}
