using EAM.WebServices;
using HGT.EAM.WebServices.Conector.Architecture.Extensions;
using HGT.EAM.WebServices.Conector.Architecture.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ServiceModel;

namespace HGT.EAM.WebServices.Conector.Architecture.Services;

public class EAMGridService : IEAMGridService
{
    private readonly GetGridDataOnlyPTClient _gridService;

    private readonly ILogger<EAMGridService> _logger;

    public EAMGridService(IConfiguration configuration, ILogger<EAMGridService> logger)
    {
        if (!configuration.GetSection("EAMBaseUrl").Exists())
            throw new InvalidOperationException("EAMBaseUrl configuration section is missing.");
        var url = configuration.GetSection("EAMBaseUrl").Value;
        if (string.IsNullOrEmpty(url))
            throw new InvalidOperationException("EAMBaseUrl configuration is not configured.");
        var binding = new BasicHttpBinding();
        binding.Security.Mode = BasicHttpSecurityMode.Transport;
        var endpointAddress = new EndpointAddress(url);
        _gridService = new GetGridDataOnlyPTClient(binding, endpointAddress);
        _logger = logger;
    }

    public async Task<GetGridDataOnlyResponseMsg> GetGridInfoAsync(GetGridDataOnlyRequestMsg request)
    {
        _logger.LogTrace($"Request trace: {Environment.NewLine} {request.GetStringXML()}");
        var response = await _gridService.GetGridDataOnlyOpAsync(request.Organization, request.Security, null, null, null, null, request.MP0116_GetGridDataOnly_001);
        _logger.LogTrace($"Response trace: {Environment.NewLine} {response.GetStringXML()}");
        return response;
    }
}
