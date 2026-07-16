using HGT.EAM.WebServices.Conector.Architecture.Interfaces;
using HGT.EAM.WebServices.Conector.Architecture.Services;
using HGT.EAM.WebServices.Infrastructure.Architecture.Models;
using HGT.EAM.WebServices.Infrastructure.Architecture.GridCache;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using HGT.EAM.WebServices.Infrastructure.Architecture.Interfaces;

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

        // Espera ante un lock de SQLite ("database is locked"), útil si dos requests coinciden en
        // cold-start. OJO: Microsoft.Data.Sqlite NO reconoce la palabra clave BusyTimeout (esa es de
        // System.Data.SQLite y aquí lanzaría "Keyword not supported"); la suya es "Default Timeout",
        // en SEGUNDOS, y es el tiempo que el proveedor reintenta ante SQLITE_BUSY/SQLITE_LOCKED antes
        // de fallar. Su default ya es 30 s; se fija explícito para poder ajustarlo por configuración
        // (GridCache:BusyTimeoutSeconds) y para que /diagnostics/sqlite reporte el valor efectivo.
        var baseConnectionString = configuration.GetConnectionString("GridCache") ?? "Data Source=gridcache.db";
        var connectionStringBuilder = new SqliteConnectionStringBuilder(baseConnectionString);
        connectionStringBuilder.DefaultTimeout =
            configuration.GetValue<int?>("GridCache:BusyTimeoutSeconds") ?? connectionStringBuilder.DefaultTimeout;
        var connectionString = connectionStringBuilder.ToString();
        services.AddDbContext<GridCacheDbContext>(options => options.UseSqlite(connectionString), ServiceLifetime.Scoped);
        services.AddScoped<IGridCacheService, GridCacheService>();
        services.AddScoped<IEamGridFetcher, EamGridFetcher>();

        return services;
    }
}