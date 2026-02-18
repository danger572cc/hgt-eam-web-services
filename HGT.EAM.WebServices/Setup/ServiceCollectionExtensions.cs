using HGT.EAM.WebServices.Conector.Architecture.Interfaces;
using HGT.EAM.WebServices.Conector.Architecture.Services;
using HGT.EAM.WebServices.Infrastructure.Architecture.Models;
using HGT.EAM.WebServices.Infrastructure.Architecture.GridCache;
using Microsoft.EntityFrameworkCore;

namespace HGT.EAM.WebServices.Setup;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var gridSettings = configuration.GetSection("EAMGrids");

        if (!gridSettings.Exists())
            throw new InvalidOperationException("EAMGrids configuration section is missing.");

        var allGrids = configuration.GetSection("EAMGrids").Get<List<EAMGridSettings>>();
        
        if (allGrids == null || allGrids.Count == 0)
            throw new InvalidOperationException("EAMGrids configuration section is missing or empty.");
        services.AddSingleton(allGrids);
        services.AddScoped<IEAMGridService, EAMGridService>();

        services.Configure<GridCacheOptions>(configuration.GetSection(GridCacheOptions.SectionName));
        var connectionString = configuration.GetConnectionString("GridCache") ?? "Data Source=gridcache.db";
        services.AddDbContext<GridCacheDbContext>(options => options.UseSqlite(connectionString), ServiceLifetime.Scoped);
        services.AddScoped<IGridCacheService, GridCacheService>();
        services.AddScoped<IEamGridFetcher, EamGridFetcher>();

        return services;
    }
}