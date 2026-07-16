using System;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.Diagnostics;

public class DiagnosticsHistory
{
    public long Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string Organization { get; set; } = string.Empty;
    public long TotalRequests { get; set; }
    public long TotalErrors { get; set; }
    public long TotalElapsedMs { get; set; }
}
