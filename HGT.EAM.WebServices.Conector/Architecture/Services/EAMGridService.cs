using EAM.WebServices;
using GridCache = EAM.WebServices.GridCache;
using HGT.EAM.WebServices.Conector.Architecture.Extensions;
using HGT.EAM.WebServices.Conector.Architecture.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Security;
using InvalidOperationException = System.InvalidOperationException;

namespace HGT.EAM.WebServices.Conector.Architecture.Services;

public class EAMGridService : IEAMGridService, IDisposable
{
    private readonly GetGridDataOnlyPTClient _gridService;

    private readonly GridCache.GetGridDataOnlyCachePTClient _gridCacheService;

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
        binding.SendTimeout = new TimeSpan(0, 30, 0);
        binding.Security.Transport = new HttpTransportSecurity { 
            ClientCredentialType = HttpClientCredentialType.None,
            ProxyCredentialType = HttpProxyCredentialType.Basic
        };
        var endpointAddress = new EndpointAddress(url);
        // certificados
        var authSecure = new X509ServiceCertificateAuthentication
        {
            CertificateValidationMode = X509CertificateValidationMode.ChainTrust,
            RevocationMode = X509RevocationMode.NoCheck, // Cambia a Online si tienes acceso total a internet
            TrustedStoreLocation = StoreLocation.LocalMachine
        };

        // Servicio base
        _gridService = new GetGridDataOnlyPTClient(binding, endpointAddress);
        _gridService.ChannelFactory.Credentials.ServiceCertificate.SslCertificateAuthentication =
        _gridService.ClientCredentials.ServiceCertificate.SslCertificateAuthentication = authSecure;
        // Servicio cache
        _gridCacheService = new GridCache.GetGridDataOnlyCachePTClient(binding, endpointAddress);
        _gridCacheService.ChannelFactory.Credentials.ServiceCertificate.SslCertificateAuthentication =
        _gridCacheService.ClientCredentials.ServiceCertificate.SslCertificateAuthentication = authSecure;
        _logger = logger;
        _logger.LogInformation("Current culture: {culture}", CultureInfo.CurrentCulture.Name);
    }

    public void Dispose()
    {
        DisposeClient(_gridService);
        DisposeClient(_gridCacheService);
        GC.SuppressFinalize(this);
    }

    public async Task<List<FIELD>> GetHeadGridAsync(GetGridDataOnlyRequestMsg request)
    {
        //total registros y definicion
        request.MP0116_GetGridDataOnly_001.FUNCTION_REQUEST_INFO.REQUEST_TYPE = FUNCTION_REQUEST_TYPE.LISTCOUNTSTORED;
        var response = await _gridService.GetGridDataOnlyOpAsync(request.Organization, request.Security, null, null, null, null, request.MP0116_GetGridDataOnly_001);
        //resultados
        var fields = response.MP0116_GetGridDataOnly_001_Result.GRIDRESULT.GRID.FIELDS.FIELD?.ToList();
        return fields;
    }

    public async Task<GridCache.MP0117_GetGridDataOnlyCache_001_ResultGRIDRESULT> GetGridCacheRowsAsync(GridCache.GetGridDataOnlyCacheRequestMsg request)
    {
        _logger.LogInformation("Request trace: {NewLine} {XML}", Environment.NewLine, request.GetStringXML());
        var response = await _gridCacheService.GetGridDataOnlyCacheOpAsync(request);
        _logger.LogInformation("Request response:  {NewLine} {XML}", Environment.NewLine, response.GetStringXML());
        //resultados
        var rows = response.MP0117_GetGridDataOnlyCache_001_Result.GRIDRESULT;
        return rows;
    }

    public async Task<Tuple<string, MP0116_GetGridDataOnly_001_ResultGRIDRESULT>> GetGridRowsAsync(GetGridDataOnlyRequestMsg request)
    {
        request.MP0116_GetGridDataOnly_001.FUNCTION_REQUEST_INFO.REQUEST_TYPE = FUNCTION_REQUEST_TYPE.LISTDATA_ONLYSTORED;
        _logger.LogInformation("Request trace: {NewLine} {XML}", Environment.NewLine, request.GetStringXML());
        var response = await _gridService.GetGridDataOnlyOpAsync(request.Organization, request.Security, null, null, null, null, request.MP0116_GetGridDataOnly_001);
        _logger.LogInformation("Request response:  {NewLine} {XML}", Environment.NewLine, response.GetStringXML());
        //resultados
        var rows = response.MP0116_GetGridDataOnly_001_Result.GRIDRESULT;
        return Tuple.Create(response.Session.sessionId, rows);
    }

    private void DisposeClient(ICommunicationObject client)
    {
        if (client == null) return;

        try
        {
            if (client.State != CommunicationState.Faulted)
            {
                client.Close();
            }
            else
            {
                client.Abort();
            }
        }
        catch (CommunicationException)
        {
            client.Abort();
        }
        catch (TimeoutException)
        {
            client.Abort();
        }
        catch (Exception)
        {
            client.Abort();
            throw;
        }
    }
}
