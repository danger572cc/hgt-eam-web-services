using HGT.EAM.WebServices.Conector.Architecture.Models;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.Interfaces;

public interface IEamGridFetcher
{
    Task<(int TotalRows, List<Field> Fields)> FetchAndCacheAsync(
        string cacheKey,
        string username,
        string organization,
        string? password,
        long gridId,
        string gridName,
        string functionName,
        int dataspyId,
        DateTime? startDate,
        DateTime? endDate,
        string filterField,
        int pageSize,
        CancellationToken cancellationToken);
}
