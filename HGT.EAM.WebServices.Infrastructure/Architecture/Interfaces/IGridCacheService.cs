using HGT.EAM.WebServices.Conector.Architecture.Models;

namespace HGT.EAM.WebServices.Conector.Architecture.Interfaces;

public interface IGridCacheService
{
    /// <summary>
    /// Computa una clave de caché única basada en los parámetros del grid.
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
    /// Inicia una nueva sesión de caché con estado "Pending".
    /// Limpia cualquier caché previo con la misma clave.
    /// </summary>
    Task BeginCacheSessionAsync(
        string cacheKey,
        List<Field> fields,
        long gridId = 0,
        string gridName = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca la sesión de caché como "Completed" indicando que la carga fue exitosa.
    /// </summary>
    Task CompleteCacheSessionAsync(
        string cacheKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega filas al caché. Debe llamarse después de BeginCacheSessionAsync.
    /// </summary>
    Task AppendCacheRowsAsync(
        string cacheKey,
        IReadOnlyList<Dictionary<string, object>> rows,
        int startIndex,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza el total de registros en el caché.
    /// </summary>
    Task UpdateTotalCountAsync(
        string cacheKey,
        int totalCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una página específica del caché.
    /// Retorna null si no existe, ha expirado o el estado no es "Completed".
    /// </summary>
    Task<ResultDataGridModel?> GetPageAsync(
        string cacheKey,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el conteo de filas cacheadas.
    /// </summary>
    Task<int> GetCachedRowCountAsync(
        string cacheKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina toda la información de caché asociada con la clave (rollback).
    /// </summary>
    Task RollbackCacheSessionAsync(
        string cacheKey,
        CancellationToken cancellationToken = default);
}