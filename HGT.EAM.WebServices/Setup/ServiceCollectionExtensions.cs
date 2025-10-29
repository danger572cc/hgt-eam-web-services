using HGT.EAM.WebServices.Conector.Architecture.Interfaces;
using HGT.EAM.WebServices.Conector.Architecture.Services;
using HGT.EAM.WebServices.Infrastructure.Architecture.Models;

namespace HGT.EAM.WebServices.Setup;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var gridSettings = configuration.GetSection("EAMGrids");

        if (!gridSettings.Exists())
            throw new InvalidOperationException("EAMGrids configuration section is missing.");

        List<EAMGridSettings> allGrids = configuration.GetSection("EAMGrids").Get<List<EAMGridSettings>>();

        if (allGrids?.Count == 0)
            throw new InvalidOperationException("EAMGrids configuration section is empty.");
        services.AddSingleton(allGrids);
        services.AddScoped<IEAMGridService, EAMGridService>();
        return services;
    }
}