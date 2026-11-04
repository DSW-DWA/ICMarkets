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
                ? Task.FromException<BlockchainData>(new HttpRequestException("upstream failed"))
                : Task.FromResult(new BlockchainData
                {
                    Height = 123,
                    AdditionalData = new Dictionary<string, System.Text.Json.JsonElement>
                    {
                        ["chain_specific"] = System.Text.Json.JsonDocument.Parse("\"value\"").RootElement.Clone()
                    }
                })),
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
        Assert.Equal(123, history[0].Data.Height);
        Assert.Equal("value", history[0].Data.AdditionalData!["chain_specific"].GetString());
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

        await repository.AddAsync(new BlockchainSnapshot(BlockchainNetwork.BitcoinMainnet, new BlockchainData { Height = 1 }, from), CancellationToken.None);
        await repository.AddAsync(new BlockchainSnapshot(BlockchainNetwork.BitcoinMainnet, new BlockchainData { Height = 2 }, from.AddDays(1)), CancellationToken.None);
        await repository.AddAsync(new BlockchainSnapshot(BlockchainNetwork.BitcoinMainnet, new BlockchainData { Height = 4 }, to), CancellationToken.None);
        await repository.AddAsync(new BlockchainSnapshot(BlockchainNetwork.DashMainnet, new BlockchainData { Height = 3 }, from.AddDays(2)), CancellationToken.None);
        await db.SaveChangesAsync();

        var history = await repository.GetHistoryAsync(
            BlockchainNetwork.BitcoinMainnet,
            from,
            to,
            10,
            CancellationToken.None);

        Assert.Equal(new long?[] { 2, 1 }, history.Select(snapshot => snapshot.Data.Height));
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
        Func<BlockchainNetwork, CancellationToken, Task<BlockchainData>> getLatestBlock) : IBlockCypherClient
    {
        public Task<BlockchainData> GetLatestBlockAsync(BlockchainNetwork network, CancellationToken cancellationToken) =>
            getLatestBlock(network, cancellationToken);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
