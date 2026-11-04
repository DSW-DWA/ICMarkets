using ICMarkets.Api.Application;
using ICMarkets.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace ICMarkets.Api.Infrastructure;

public sealed class BlockchainDbContext(DbContextOptions<BlockchainDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<BlockchainSnapshot> Snapshots => Set<BlockchainSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<BlockchainSnapshot>();
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Network).HasConversion<string>().HasMaxLength(32).IsRequired();
        var dataConverter = new ValueConverter<BlockchainData, string>(
            data => JsonSerializer.Serialize(data, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<BlockchainData>(json, (JsonSerializerOptions?)null) ?? new BlockchainData());
        entity.Property(x => x.Data).HasConversion(dataConverter).HasColumnName("DataJson").IsRequired();
        entity.Property(x => x.CreatedAt).IsRequired();
        entity.HasIndex(x => new { x.Network, x.CreatedAt });
    }
}

public sealed class BlockchainSnapshotRepository(BlockchainDbContext dbContext) : IBlockchainSnapshotRepository
{
    public Task AddAsync(BlockchainSnapshot snapshot, CancellationToken cancellationToken) =>
        dbContext.Snapshots.AddAsync(snapshot, cancellationToken).AsTask();

    public async Task<IReadOnlyList<BlockchainSnapshot>> GetHistoryAsync(BlockchainNetwork network, DateTimeOffset from, DateTimeOffset to, int limit, CancellationToken cancellationToken) =>
        await dbContext.Snapshots.AsNoTracking()
            .Where(x => x.Network == network)
            .Where(x => x.CreatedAt >= from.UtcDateTime && x.CreatedAt < to.UtcDateTime)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
}
