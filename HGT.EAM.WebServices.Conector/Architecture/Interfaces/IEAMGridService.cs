using EAM.WebServices;
using System.Security.Claims;

namespace HGT.EAM.WebServices.Conector.Architecture.Interfaces;

public interface IEAMGridService
{
    Task<Tuple<int, List<FIELD>, MP0116_GetGridDataOnly_001_ResultGRIDRESULT>> GetGridInfoAsync(GetGridDataOnlyRequestMsg request);
}
