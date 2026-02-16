using Microsoft.EntityFrameworkCore;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.GridCache;

public class GridCacheDbContext : DbContext
{
    public GridCacheDbContext(DbContextOptions<GridCacheDbContext> options) : base(options) { }

    public DbSet<GridCacheEntry> GridCacheEntries => Set<GridCacheEntry>();
    public DbSet<GridCacheFieldEntity> GridCacheFields => Set<GridCacheFieldEntity>();
    public DbSet<GridCacheRowEntity> GridCacheRows => Set<GridCacheRowEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GridCacheEntry>(e =>
        {
            e.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<GridCacheFieldEntity>(e =>
        {
            e.HasKey(x => new { x.CacheKey, x.Id });
        });

        modelBuilder.Entity<GridCacheRowEntity>(e =>
        {
            e.HasKey(x => new { x.CacheKey, x.RowIndex });
        });
    }
}
