using HGT.EAM.WebServices.Conector.Architecture.Models;

namespace HGT.EAM.WebServices.Conector.Architecture.Interfaces;

/// <summary>
/// Servicio de caché genérico para resultados de grids EAM.
/// Almacena el grid completo en SQLite y permite paginar on-demand sin volver a llamar al SOAP.
/// </summary>
public interface IGridCacheService
{
    /// <summary>
    /// Genera una clave única por usuario y por conjunto de parámetros del grid (organización, grid, dataspy, rango de fechas, etc.).
    /// El caché es por usuario: cada quien ve solo sus datos cacheados.
    /// </summary>
    string ComputeCacheKey(
        string username,
        string organization,
        int gridId,
        string gridName,
        string functionName,
        int dataspyId,
        DateTime? startDate,
        DateTime? endDate,
        string filterField);

    /// <summary>
    /// Obtiene una página del grid desde el caché, si existe y no ha expirado.
    /// </summary>
    /// <returns>El modelo paginado o null si no hay caché válido.</returns>
    Task<ResultDataGridModel?> GetPageAsync(
        string cacheKey,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inicia una sesión de guardado en caché, limpiando datos previos y estableciendo metadatos.
    /// </summary>
    Task BeginCacheSessionAsync(
        string cacheKey,
        int totalCount,
        List<Field> fields,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega un lote de filas al caché existente.
    /// </summary>
    Task AppendCacheRowsAsync(
        string cacheKey,
        IReadOnlyList<Dictionary<string, object>> rows,
        int startIndex,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el número total de filas cacheadas para una clave específica.
    /// Útil para validar integridad de datos después del fetch.
    /// </summary>
    Task<int> GetCachedRowCountAsync(
        string cacheKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina todos los datos de caché para una sesión específica.
    /// Se utiliza para rollback en caso de errores durante el fetch.
    /// </summary>
    Task RollbackCacheSessionAsync(
        string cacheKey,
        CancellationToken cancellationToken = default);
}
