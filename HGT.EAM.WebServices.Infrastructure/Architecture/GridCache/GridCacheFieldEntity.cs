using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.GridCache;

/// <summary>
/// Definición de una columna del grid en caché.
/// </summary>
[Table("GridCacheFields")]
public class GridCacheFieldEntity
{
    [Key]
    [Column(Order = 0)]
    [MaxLength(64)]
    public string CacheKey { get; set; } = string.Empty;

    [Key]
    [Column(Order = 1)]
    public int Id { get; set; }

    [MaxLength(256)]
    public string Label { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    public int Order { get; set; }

    [MaxLength(64)]
    public string Type { get; set; } = string.Empty;

    public bool Visible { get; set; }

    public int Width { get; set; }

    [ForeignKey(nameof(CacheKey))]
    public GridCacheEntry? Entry { get; set; }
}
