using EAM.WebServices;

namespace HGT.EAM.WebServices.Conector.Architecture.Interfaces;

public interface IEAMGridService
{
    Task<GetGridDataOnlyResponseMsg> GetGridInfoAsync(GetGridDataOnlyRequestMsg request);
}
