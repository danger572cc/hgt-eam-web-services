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

    [MaxLength(100)]
    public string GridName { get; set; } = string.Empty;

    public int? TotalCount { get; set; }

    /// <summary>
    /// Estado del caché: Pending, Completed, Failed
    /// Pending = En proceso de carga
    /// Completed = Carga exitosa, datos válidos
    /// Failed = Falló durante la carga, debe limpiarse
    /// </summary>
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; }
}
