using HGT.EAM.WebServices.Conector.Architecture.Interfaces;
using HGT.EAM.WebServices.Conector.Architecture.Models;
using HGT.EAM.WebServices.Infrastructure.Architecture.GridCache;
using HGT.EAM.WebServices.Infrastructure.Architecture.Query;

using Mediator;

namespace HGT.EAM.WebServices.Application.Queries;

public class GridDataOnlyGetQueryHandler : IRequestHandler<GridDataOnlyGetQuery, ResultDataGridModel>
{
    private readonly IGridCacheService _cache;
    private readonly IEamGridFetcher _fetcher;

    public GridDataOnlyGetQueryHandler(
        IGridCacheService cache,
        IEamGridFetcher fetcher)
    {
        _cache = cache;
        _fetcher = fetcher;
    }

    public async ValueTask<ResultDataGridModel> Handle(GridDataOnlyGetQuery command, CancellationToken cancellationToken)
    {
        var cacheKey = _cache.ComputeCacheKey(
            command.Username,
            command.Organization,
            command.GridId,
            command.GridName,
            command.FunctionName,
            command.DataspyId,
            command.StartDate,
            command.EndDate,
            command.FilterField);

        var page = command.Page > 0 ? command.Page : 1;
        var pageSize = command.NumberOfRowsFirstReturned;

        // 1. Intentar obtener de la caché
        var cached = await _cache.GetPageAsync(cacheKey, page, pageSize, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        // 2. Recuperar y almacenar en caché (vía Streaming/Flujo)
        var (totalRows, fields) = await _fetcher.FetchAndCacheAsync(
            cacheKey,
            command.Username,
            command.Organization,
            command.Password,
            command.GridId,
            command.GridName,
            command.FunctionName,
            command.DataspyId,
            command.StartDate,
            command.EndDate,
            command.FilterField,
            pageSize,
            cancellationToken);

        // 3. Retornar desde la caché (ahora está garantizado que los datos se encuentran allí)
        // Para separar estrictamente las responsabilidades, podemos volver a consultar la caché. 
        // SQLite es lo suficientemente rápido para este patrón de "Leer lo que escribes" (Read-Your-Writes) en una sola página.
        // Esto mantiene la lógica del manejador pura: Comprobar Caché -> Error de lectura -> Llenar Caché -> Leer Caché.

        var freshResult = await _cache.GetPageAsync(cacheKey, page, pageSize, cancellationToken);
        if (freshResult != null)
        {
            return freshResult;
        }

        // Plan de respaldo (no debería ocurrir si la descarga funcionó correctamente)
        var totalPages = (int)Math.Ceiling((double)totalRows / pageSize);
        return new ResultDataGridModel
        {
            TotalRecords = totalRows,
            TotalPages = totalPages,
            CurrentPage = page,
            TotalRecordsReturned = 0,
            DataRecord = new DataRecord { Fields = fields, Rows = [] }
        };
    }
}
