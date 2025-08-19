using EAM.WebServices;
using HGT.EAM.WebServices.Conector.Architecture.Interfaces;
using System.ServiceModel;

namespace HGT.EAM.WebServices.Conector.Architecture.Services;

public class AccountsPayableService : IAccountsPayableService
{
    private readonly GetGridDataOnlyPTClient _gridService;

    public AccountsPayableService()
    {
        _gridService = new GetGridDataOnlyPTClient();
        _gridService.Endpoint.Address = new EndpointAddress("https://eamdev.hgtlatam.com/axis/services/EWSConnector");
    }

    public async Task GetInvoiceReceiptsEcuadorAsync(string organization, bool previousDay = false, bool previousMonth = false, bool previousYear = false, bool allRecords = true)
    {
        
    }

    public async Task GetOCFinanceViewAsync(string organization, bool previousDay = false, bool previousMonth = false, bool previousYear = false, bool allRecords = true)
    {
        
    }
}
