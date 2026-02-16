using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HGT.EAM.WebServices.Conector.Architecture.Interfaces;
using HGT.EAM.WebServices.Conector.Architecture.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.GridCache;

public class GridCacheService : IGridCacheService
{
    private readonly GridCacheDbContext _db;
    private readonly ILogger<GridCacheService> _logger;
    private readonly GridCacheOptions _options;

    public GridCacheService(
        GridCacheDbContext db,
        ILogger<GridCacheService> logger,
        IOptions<GridCacheOptions> options)
    {
        _db = db;
        _logger = logger;
        _options = options.Value;
    }

    public string ComputeCacheKey(
        string username,
        string organization,
        int gridId,
        string gridName,
        string functionName,
        int dataspyId,
        DateTime? startDate,
        DateTime? endDate,
        string filterField)
    {
        var payload = $"{username ?? ""}|{organization}|{gridId}|{gridName}|{functionName}|{dataspyId}|{startDate:O}|{endDate:O}|{filterField ?? ""}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes)[..64];
    }

    public async Task<ResultDataGridModel?> GetPageAsync(
        string cacheKey,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return null;

        var entry = await _db.GridCacheEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.CacheKey == cacheKey, cancellationToken);

        if (entry == null)
            return null;

        if (_options.ExpirationMinutes > 0 && (DateTime.UtcNow - entry.CreatedAt).TotalMinutes > _options.ExpirationMinutes)
        {
            _logger.LogDebug("Grid cache expired for key {CacheKey}", cacheKey);
            await RemoveCacheAsync(cacheKey, cancellationToken);
            return null;
        }

        var fields = await _db.GridCacheFields
            .AsNoTracking()
            .Where(f => f.CacheKey == cacheKey)
            .OrderBy(f => f.Order)
            .Select(f => new Field
            {
                Id = f.Id,
                Label = f.Label,
                Name = f.Name,
                Order = f.Order,
                Type = f.Type,
                Visible = f.Visible,
                Width = f.Width
            })
            .ToListAsync(cancellationToken);

        var skip = (page - 1) * pageSize;
        var rowEntities = await _db.GridCacheRows
            .AsNoTracking()
            .Where(r => r.CacheKey == cacheKey)
            .OrderBy(r => r.RowIndex)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var rows = new List<Dictionary<string, object>>(rowEntities.Count);
        foreach (var re in rowEntities)
        {
            var dict = DeserializeRow(re.RowData);
            if (dict != null)
                rows.Add(dict);
        }

        var totalPages = (int)Math.Ceiling((double)entry.TotalCount / pageSize);
        return new ResultDataGridModel
        {
            TotalRecords = entry.TotalCount,
            TotalPages = totalPages,
            CurrentPage = page,
            TotalRecordsReturned = rows.Count,
            DataRecord = new DataRecord
            {
                Fields = fields,
                Rows = rows
            }
        };
    }

    public async Task BeginCacheSessionAsync(
        string cacheKey,
        int totalCount,
        List<Field> fields,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return;

        await RemoveCacheAsync(cacheKey, cancellationToken);

        var entry = new GridCacheEntry
        {
            CacheKey = cacheKey,
            GridId = 0,
            GridName = "",
            TotalCount = totalCount,
            CreatedAt = DateTime.UtcNow
        };
        _db.GridCacheEntries.Add(entry);

        foreach (var f in fields)
        {
            _db.GridCacheFields.Add(new GridCacheFieldEntity
            {
                CacheKey = cacheKey,
                Id = f.Id,
                Label = f.Label,
                Name = f.Name,
                Order = f.Order,
                Type = f.Type,
                Visible = f.Visible,
                Width = f.Width
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AppendCacheRowsAsync(
        string cacheKey,
        IReadOnlyList<Dictionary<string, object>> rows,
        int startIndex,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return;

        for (var i = 0; i < rows.Count; i++)
        {
            _db.GridCacheRows.Add(new GridCacheRowEntity
            {
                CacheKey = cacheKey,
                RowIndex = startIndex + i,
                RowData = SerializeRow(rows[i])
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetCachedRowCountAsync(
        string cacheKey,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return 0;

        return await _db.GridCacheRows
            .Where(r => r.CacheKey == cacheKey)
            .CountAsync(cancellationToken);
    }

    public async Task RollbackCacheSessionAsync(
        string cacheKey,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return;

        _logger.LogWarning("Rolling back cache session for key {CacheKey}", cacheKey);
        await RemoveCacheAsync(cacheKey, cancellationToken);
    }

    private async Task RemoveCacheAsync(string cacheKey, CancellationToken cancellationToken)
    {
        await _db.GridCacheRows.Where(r => r.CacheKey == cacheKey).ExecuteDeleteAsync(cancellationToken);
        await _db.GridCacheFields.Where(f => f.CacheKey == cacheKey).ExecuteDeleteAsync(cancellationToken);
        await _db.GridCacheEntries.Where(e => e.CacheKey == cacheKey).ExecuteDeleteAsync(cancellationToken);
    }

    private static string SerializeRow(Dictionary<string, object> row)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var (k, v) in row)
            dict[k] = v;
        return JsonSerializer.Serialize(dict);
    }

    private static Dictionary<string, object>? DeserializeRow(string json)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (dict == null) return null;
            var result = new Dictionary<string, object>();
            foreach (var (k, v) in dict)
                result[k] = JsonElementToObject(v);
            return result;
        }
        catch(Exception ex)
        {
             // Log error silently? Ideally we should inject logger here or handle upstream
             // For now, keeping original behavior but safer
            return null;
        }
    }

    private static object JsonElementToObject(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString() ?? string.Empty,
            JsonValueKind.Number => el.TryGetInt64(out var i) ? i : el.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => (object?)null!,
            _ => el.GetRawText()
        };
    }
}
