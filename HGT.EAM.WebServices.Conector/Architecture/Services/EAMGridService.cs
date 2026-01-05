using EAM.WebServices;
using HGT.EAM.WebServices.Conector.Architecture.Extensions;
using HGT.EAM.WebServices.Conector.Architecture.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Text.RegularExpressions;
using InvalidOperationException = System.InvalidOperationException;

namespace HGT.EAM.WebServices.Conector.Architecture.Services;

public class EAMGridService : IEAMGridService, IDisposable
{
    private readonly CultureInfo _culture;

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
        binding.SendTimeout = new TimeSpan(5, 0, 0);
        binding.Security.Transport = new HttpTransportSecurity { 
            ClientCredentialType = HttpClientCredentialType.None,
            ProxyCredentialType = HttpProxyCredentialType.Basic
        };
        var endpointAddress = new EndpointAddress(url);
        _gridService = new GetGridDataOnlyPTClient(binding, endpointAddress);
        _gridService.ChannelFactory.Credentials.ServiceCertificate.SslCertificateAuthentication =
                _gridService.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
                    new X509ServiceCertificateAuthentication
                    {
                        CertificateValidationMode = X509CertificateValidationMode.None,
                        RevocationMode = X509RevocationMode.NoCheck,
                        TrustedStoreLocation = StoreLocation.LocalMachine
                    };
        _logger = logger;

        _logger.LogInformation($"Current culture: {CultureInfo.CurrentCulture.Name}");
    }

    public void Dispose()
    {
        _gridService.Close();
        GC.SuppressFinalize(this);
    }

    public async Task<MP0116_GetGridDataOnly_001_ResultGRIDRESULT> GetGridRowsAsync(GetGridDataOnlyRequestMsg request)
    {
        //datos
        request.MP0116_GetGridDataOnly_001.FUNCTION_REQUEST_INFO.REQUEST_TYPE = FUNCTION_REQUEST_TYPE.LISTDATA_ONLYSTORED;
        _logger.LogInformation($"Request trace: {Environment.NewLine} {request.GetStringXML()}");
        var response = await _gridService.GetGridDataOnlyOpAsync(request.Organization, request.Security, null, null, null, null, request.MP0116_GetGridDataOnly_001);
        _logger.LogInformation($"Request response: {Environment.NewLine} {response.GetStringXML()}");
        //resultados
        var rows = response.MP0116_GetGridDataOnly_001_Result.GRIDRESULT;
        return rows;
    }

    public async Task<Tuple<int, List<FIELD>>> GetHeadGridAsync(GetGridDataOnlyRequestMsg request)
    {
        //total registros y definicion
        request.MP0116_GetGridDataOnly_001.FUNCTION_REQUEST_INFO.REQUEST_TYPE = FUNCTION_REQUEST_TYPE.LISTCOUNTSTORED;
        _logger.LogInformation($"Request count trace: {Environment.NewLine} {request.GetStringXML()}");
        var countResponse = await _gridService.GetGridDataOnlyOpAsync(request.Organization, request.Security, null, null, null, null, request.MP0116_GetGridDataOnly_001);
        _logger.LogInformation($"Total records obtained: {countResponse.MP0116_GetGridDataOnly_001_Result.GRIDRESULT.GRID.TOTALCOUNT}");
        //resultados
        int.TryParse(countResponse.MP0116_GetGridDataOnly_001_Result.GRIDRESULT.GRID.TOTALCOUNT.Replace(".", "").Replace(",", "").Trim(), out int totalRows);
        _logger.LogInformation($"Value var totalRows: {totalRows}");
        var fields = countResponse.MP0116_GetGridDataOnly_001_Result.GRIDRESULT.GRID.FIELDS.FIELD?.ToList();
        return new Tuple<int, List<FIELD>>(totalRows, fields);
    }
}
