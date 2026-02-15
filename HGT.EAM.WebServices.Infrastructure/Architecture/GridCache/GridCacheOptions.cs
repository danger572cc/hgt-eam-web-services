namespace HGT.EAM.WebServices.Infrastructure.Architecture.GridCache;

public class GridCacheOptions
{
    public const string SectionName = "GridCache";

    /// <summary>
    /// Habilita o deshabilita el caché en SQLite. Por defecto true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Minutos hasta que una entrada de caché se considere expirada. 0 = no expira.
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;
}
