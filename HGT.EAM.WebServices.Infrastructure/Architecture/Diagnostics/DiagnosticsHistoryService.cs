using HGT.EAM.WebServices.Infrastructure.Architecture.GridCache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.Diagnostics;

public class DiagnosticsHistoryService : BackgroundService
{
    private readonly DiagnosticsMetrics _metrics;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DiagnosticsHistoryService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public DiagnosticsHistoryService(DiagnosticsMetrics metrics, IServiceProvider serviceProvider, ILogger<DiagnosticsHistoryService> logger)
    {
        _metrics = metrics;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await SaveMetricsAsync("Periodic");
            }
        }
        catch (OperationCanceledException)
        {
            // Apagado
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await SaveMetricsAsync("Shutdown");
        await base.StopAsync(cancellationToken);
    }

    private async Task SaveMetricsAsync(string trigger)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GridCacheDbContext>();

            var now = DateTime.UtcNow;

            // Global
            dbContext.DiagnosticsHistory.Add(new DiagnosticsHistory
            {
                TimestampUtc = now,
                Organization = "GLOBAL",
                TotalRequests = _metrics.TotalRequests,
                TotalErrors = _metrics.TotalErrors,
                TotalElapsedMs = _metrics.TotalElapsedMs
            });

            // Per tenant
            foreach (var kvp in _metrics.Tenants)
            {
                dbContext.DiagnosticsHistory.Add(new DiagnosticsHistory
                {
                    TimestampUtc = now,
                    Organization = kvp.Key,
                    TotalRequests = kvp.Value.TotalRequests,
                    TotalErrors = kvp.Value.TotalErrors,
                    TotalElapsedMs = kvp.Value.TotalElapsedMs
                });
            }

            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Guardado snapshot de diagnósticos en SQLite ({Trigger})", trigger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al persistir métricas de diagnóstico en SQLite");
        }
    }
}
