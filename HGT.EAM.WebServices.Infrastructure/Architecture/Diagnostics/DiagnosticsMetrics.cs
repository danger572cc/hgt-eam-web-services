using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.Diagnostics;

public class TenantMetrics
{
    private long _total;
    private long _errors;
    private long _elapsedMsTotal;

    public void Record(long elapsedMs, bool isError)
    {
        Interlocked.Increment(ref _total);
        Interlocked.Add(ref _elapsedMsTotal, elapsedMs);
        if (isError)
            Interlocked.Increment(ref _errors);
    }

    public long TotalRequests => Interlocked.Read(ref _total);
    public long TotalErrors => Interlocked.Read(ref _errors);
    public long TotalElapsedMs => Interlocked.Read(ref _elapsedMsTotal);
}

/// <summary>
/// Contadores livianos en memoria para el panel de diagnóstico (/diagnostics).
/// Singleton; actualizado por <c>DiagnosticsMetricsMiddleware</c> en cada request.
/// No persiste: se reinicia con la aplicación (mide "desde el último arranque").
/// </summary>
public sealed class DiagnosticsMetrics
{
    private long _total;
    private long _errors;
    private long _elapsedMsTotal;

    private readonly ConcurrentDictionary<string, TenantMetrics> _tenants = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Momento (UTC) en que arrancó el proceso/host.</summary>
    public DateTime StartedUtc { get; } = DateTime.UtcNow;

    /// <summary>Registra un request atendido. <paramref name="isError"/> = respuesta 5xx.</summary>
    public void Record(string organization, long elapsedMs, bool isError)
    {
        Interlocked.Increment(ref _total);
        Interlocked.Add(ref _elapsedMsTotal, elapsedMs);
        if (isError)
            Interlocked.Increment(ref _errors);

        if (!string.IsNullOrWhiteSpace(organization))
        {
            var tenant = _tenants.GetOrAdd(organization, _ => new TenantMetrics());
            tenant.Record(elapsedMs, isError);
        }
    }

    public long TotalRequests => Interlocked.Read(ref _total);
    public long TotalErrors => Interlocked.Read(ref _errors);
    public long TotalElapsedMs => Interlocked.Read(ref _elapsedMsTotal);
    public double AverageMs => TotalRequests == 0 ? 0 : (double)TotalElapsedMs / TotalRequests;
    public TimeSpan Uptime => DateTime.UtcNow - StartedUtc;

    public IReadOnlyDictionary<string, TenantMetrics> Tenants => _tenants;
}
