using HGT.EAM.WebServices.Infrastructure.Architecture.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.Middlewares;

/// <summary>
/// Cronometra cada request y alimenta <see cref="DiagnosticsMetrics"/> (contadores de
/// rendimiento del panel /diagnostics). Se excluye a sí mismo (/diagnostics) para que el
/// sondeo del panel no distorsione las métricas.
/// </summary>
public class DiagnosticsMetricsMiddleware(RequestDelegate next, DiagnosticsMetrics metrics)
{
    private readonly RequestDelegate _next = next;
    private readonly DiagnosticsMetrics _metrics = metrics;

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/diagnostics"))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            _metrics.Record(stopwatch.ElapsedMilliseconds, context.Response.StatusCode >= 500);
        }
    }
}
