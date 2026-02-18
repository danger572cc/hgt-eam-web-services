using EAM.WebServices;
using GridCache = EAM.WebServices.GridCache;

namespace HGT.EAM.WebServices.Conector.Architecture.Interfaces;

public interface IEAMGridService
{
    Task<List<FIELD>> GetHeadGridAsync(GetGridDataOnlyRequestMsg request);
    Task<GridCache.MP0117_GetGridDataOnlyCache_001_ResultGRIDRESULT> GetGridCacheRowsAsync(GridCache.GetGridDataOnlyCacheRequestMsg request);

    Task<Tuple<string, MP0116_GetGridDataOnly_001_ResultGRIDRESULT>> GetGridRowsAsync(GetGridDataOnlyRequestMsg request);
}
