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
    /// Guarda el resultado completo del grid en SQLite para poder paginar después desde caché.
    /// </summary>
    Task SaveFullGridAsync(
        string cacheKey,
        int totalCount,
        List<Field> fields,
        IReadOnlyList<Dictionary<string, object>> rows,
        CancellationToken cancellationToken = default);
}
