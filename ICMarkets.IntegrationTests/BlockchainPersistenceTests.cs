using ICMarkets.Api.Application;
using ICMarkets.Api.Domain;
using ICMarkets.Api.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ICMarkets.IntegrationTests;

public sealed class BlockchainPersistenceTests
{
    [Fact]
    public async Task CollectAllAsync_PersistsSuccessfulNetworkResultsToSqlite()
    {
        await using var connection = await OpenDatabaseAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var failedNetwork = BlockchainNetwork.BitcoinTestnet3;
        var repository = new BlockchainSnapshotRepository(db);
        var service = new BlockchainService(
            new FakeBlockCypherClient((network, _) => network == failedNetwork
                ? Task.FromException<string>(new HttpRequestException("upstream failed"))
                : Task.FromResult("{\"height\":123}")),
            repository,
            db,
            new FixedTimeProvider(new DateTimeOffset(2026, 11, 4, 0, 0, 0, TimeSpan.Zero)),
            NullLogger<BlockchainService>.Instance);

        var outcomes = await service.CollectAllAsync(CancellationToken.None);
        var history = await repository.GetHistoryAsync(
            BlockchainNetwork.BitcoinMainnet,
            new DateTimeOffset(2026, 11, 3, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 5, 0, 0, 0, TimeSpan.Zero),
            10,
            CancellationToken.None);

        Assert.Equal(Enum.GetValues<BlockchainNetwork>().Length - 1, outcomes.Count(outcome => outcome.Succeeded));
        Assert.Single(outcomes, outcome => !outcome.Succeeded);
        Assert.Single(history);
        Assert.Equal("{\"height\":123}", history[0].ResponseJson);
    }

    [Fact]
    public async Task GetHistoryAsync_FiltersNetworkAndHalfOpenDateRangeAndSortsNewestFirst()
    {
        await using var connection = await OpenDatabaseAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var repository = new BlockchainSnapshotRepository(db);
        var from = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 11, 4, 0, 0, 0, TimeSpan.Zero);

        await repository.AddAsync(new BlockchainSnapshot(BlockchainNetwork.BitcoinMainnet, "{\"height\":1}", from), CancellationToken.None);
        await repository.AddAsync(new BlockchainSnapshot(BlockchainNetwork.BitcoinMainnet, "{\"height\":2}", from.AddDays(1)), CancellationToken.None);
        await repository.AddAsync(new BlockchainSnapshot(BlockchainNetwork.BitcoinMainnet, "{\"height\":4}", to), CancellationToken.None);
        await repository.AddAsync(new BlockchainSnapshot(BlockchainNetwork.DashMainnet, "{\"height\":3}", from.AddDays(2)), CancellationToken.None);
        await db.SaveChangesAsync();

        var history = await repository.GetHistoryAsync(
            BlockchainNetwork.BitcoinMainnet,
            from,
            to,
            10,
            CancellationToken.None);

        Assert.Equal(new[] { "{\"height\":2}", "{\"height\":1}" }, history.Select(snapshot => snapshot.ResponseJson));
    }

    private static async Task<SqliteConnection> OpenDatabaseAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    private static BlockchainDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<BlockchainDbContext>()
            .UseSqlite(connection)
            .Options;
        return new BlockchainDbContext(options);
    }

    private sealed class FakeBlockCypherClient(
        Func<BlockchainNetwork, CancellationToken, Task<string>> getLatestBlock) : IBlockCypherClient
    {
        public Task<string> GetLatestBlockAsync(BlockchainNetwork network, CancellationToken cancellationToken) =>
            getLatestBlock(network, cancellationToken);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
