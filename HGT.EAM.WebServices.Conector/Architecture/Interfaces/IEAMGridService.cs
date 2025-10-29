using EAM.WebServices;

namespace HGT.EAM.WebServices.Conector.Architecture.Interfaces;

public interface IEAMGridService
{
    Task<MP0116_GetGridDataOnly_001_ResultGRIDRESULT> GetGridRowsAsync(GetGridDataOnlyRequestMsg request);

    Task<Tuple<int, List<FIELD>>>GetHeadGridAsync(GetGridDataOnlyRequestMsg request);
}
