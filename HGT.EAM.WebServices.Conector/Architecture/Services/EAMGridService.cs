using EAM.WebServices;
using HGT.EAM.WebServices.Conector.Architecture.Extensions;
using HGT.EAM.WebServices.Conector.Architecture.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.ServiceModel;

namespace HGT.EAM.WebServices.Conector.Architecture.Services;

public class EAMGridService : IEAMGridService
{
    private readonly GetGridDataOnlyPTClient _gridService;

    private readonly ILogger<EAMGridService> _logger;

    public EAMGridService(
        IConfiguration configuration, 
        ILogger<EAMGridService> logger)
    {
        if (!configuration.GetSection("EAMBaseUrl").Exists())
            throw new InvalidOperationException("EAMBaseUrl configuration section is missing.");
        var url = configuration.GetSection("EAMBaseUrl").Value;
        if (string.IsNullOrEmpty(url))
            throw new InvalidOperationException("EAMBaseUrl configuration is not configured.");
        var binding = new BasicHttpBinding();
        binding.Security.Mode = BasicHttpSecurityMode.Transport;
        binding.MaxReceivedMessageSize = 10000000;
        binding.SendTimeout = new TimeSpan(0, 10, 0);
        var endpointAddress = new EndpointAddress(url);
        _gridService = new GetGridDataOnlyPTClient(binding, endpointAddress);
        _logger = logger;
    }

    public async Task<Tuple<int, List<FIELD>, MP0116_GetGridDataOnly_001_ResultGRIDRESULT>> GetGridInfoAsync(GetGridDataOnlyRequestMsg request)
    {
        _logger.LogInformation($"Request trace: {Environment.NewLine} {request.GetStringXML()}");
        //total registros y definicion
        request.MP0116_GetGridDataOnly_001.FUNCTION_REQUEST_INFO.REQUEST_TYPE = FUNCTION_REQUEST_TYPE.LISTCOUNTSTORED;
        var countResponse = await _gridService.GetGridDataOnlyOpAsync(request.Organization, request.Security, null, null, null, null, request.MP0116_GetGridDataOnly_001);
        //datos
        request.MP0116_GetGridDataOnly_001.FUNCTION_REQUEST_INFO.REQUEST_TYPE = FUNCTION_REQUEST_TYPE.LISTDATA_ONLYSTORED;
        var response = await _gridService.GetGridDataOnlyOpAsync(request.Organization, request.Security, null, null, null, null, request.MP0116_GetGridDataOnly_001);
        _logger.LogInformation($"Response trace: {Environment.NewLine} {response.GetStringXML()}");
        //resultados
        var culture = CultureInfo.GetCultureInfo("es-ES");
        int.TryParse(countResponse.MP0116_GetGridDataOnly_001_Result.GRIDRESULT.GRID.TOTALCOUNT, NumberStyles.AllowThousands, culture, out int totalRows);
        var fields = countResponse.MP0116_GetGridDataOnly_001_Result.GRIDRESULT.GRID.FIELDS.FIELD?.ToList();
        var rows = response.MP0116_GetGridDataOnly_001_Result.GRIDRESULT;
        return new Tuple<int, List<FIELD>, MP0116_GetGridDataOnly_001_ResultGRIDRESULT>(totalRows, fields, rows);
    }
}
