using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.GridCache;

/// <summary>
/// Una fila del grid en caché (datos como JSON para ser genérico).
/// </summary>
[Table("GridCacheRows")]
public class GridCacheRowEntity
{
    [MaxLength(64)]
    public string CacheKey { get; set; } = string.Empty;

    public int RowIndex { get; set; }

    /// <summary>
    /// Fila serializada como JSON: Dictionary&lt;string, object&gt; (nombre de campo -> valor).
    /// </summary>
    public string RowData { get; set; } = string.Empty;

    [ForeignKey(nameof(CacheKey))]
    public GridCacheEntry? Entry { get; set; }
}
