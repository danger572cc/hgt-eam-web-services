using System;
using System.Threading;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.Diagnostics;

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

    /// <summary>Momento (UTC) en que arrancó el proceso/host.</summary>
    public DateTime StartedUtc { get; } = DateTime.UtcNow;

    /// <summary>Registra un request atendido. <paramref name="isError"/> = respuesta 5xx.</summary>
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
    public double AverageMs => TotalRequests == 0 ? 0 : (double)TotalElapsedMs / TotalRequests;
    public TimeSpan Uptime => DateTime.UtcNow - StartedUtc;
}
