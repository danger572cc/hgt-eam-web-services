using System.ComponentModel.DataAnnotations;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.GridCache;

/// <summary>
/// Entrada de caché para un grid completo (metadatos).
/// </summary>
public class GridCacheEntry
{
    [Key]
    [MaxLength(64)]
    public string CacheKey { get; set; } = string.Empty;

    public long GridId { get; set; }

    [MaxLength(256)]
    public string GridName { get; set; } = string.Empty;

    public int? TotalCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
