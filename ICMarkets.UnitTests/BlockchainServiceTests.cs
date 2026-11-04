using ICMarkets.Api.Application;
using ICMarkets.Api.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ICMarkets.UnitTests;

public sealed class BlockchainServiceTests
{
    [Fact]
    public async Task CollectAllAsync_PartialFailure_SavesSuccessfulNetworksAndReturnsEveryOutcome()
    {
        var failedNetwork = BlockchainNetwork.BitcoinTestnet3;
        var client = new FakeBlockCypherClient((network, _) => network == failedNetwork
            ? Task.FromException<BlockchainData>(new HttpRequestException("upstream failed"))
            : Task.FromResult(new BlockchainData { Height = 123 }));
        var repository = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(client, repository, unitOfWork);

        var outcomes = await service.CollectAllAsync(CancellationToken.None);

        Assert.Equal(Enum.GetValues<BlockchainNetwork>().Length, outcomes.Count);
        var failedOutcome = Assert.Single(outcomes, outcome => !outcome.Succeeded);
        Assert.Equal(failedNetwork, failedOutcome.Network);
        Assert.Equal(Enum.GetValues<BlockchainNetwork>().Length - 1, repository.AddedSnapshots.Count);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        Assert.Equal(
            Enum.GetValues<BlockchainNetwork>().Where(network => network != failedNetwork).Order(),
            repository.AddedSnapshots.Select(snapshot => snapshot.Network).Order());
        Assert.Equal(repository.AddedSnapshots.Count, repository.AddedSnapshots.Select(snapshot => snapshot.CreatedAt).Distinct().Count());
    }

    [Fact]
    public async Task CollectAllAsync_AllNetworksFail_DoesNotCallSaveChanges()
    {
        var client = new FakeBlockCypherClient((_, _) =>
            Task.FromException<BlockchainData>(new HttpRequestException("upstream failed")));
        var repository = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(client, repository, unitOfWork);

        var outcomes = await service.CollectAllAsync(CancellationToken.None);

        Assert.All(outcomes, outcome => Assert.False(outcome.Succeeded));
        Assert.Empty(repository.AddedSnapshots);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task CollectAllAsync_InvalidJson_FailsOnlyThatNetwork()
    {
        var failedNetwork = BlockchainNetwork.DashMainnet;
        var client = new FakeBlockCypherClient((network, _) =>
            network == failedNetwork
                ? Task.FromException<BlockchainData>(new InvalidUpstreamResponseException("invalid json"))
                : Task.FromResult(new BlockchainData { Height = 123 }));
        var repository = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(client, repository, unitOfWork);

        var outcomes = await service.CollectAllAsync(CancellationToken.None);

        Assert.Equal(failedNetwork, Assert.Single(outcomes, outcome => !outcome.Succeeded).Network);
        Assert.Equal(Enum.GetValues<BlockchainNetwork>().Length - 1, repository.AddedSnapshots.Count);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task CollectAllAsync_Cancellation_PropagatesAndDoesNotSave()
    {
        using var cancellation = new CancellationTokenSource();
        var client = new FakeBlockCypherClient((_, token) =>
        {
            cancellation.Cancel();
            return Task.FromCanceled<BlockchainData>(token);
        });
        var repository = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(client, repository, unitOfWork);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CollectAllAsync(cancellation.Token));

        Assert.Empty(repository.AddedSnapshots);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    private static BlockchainService CreateService(
        IBlockCypherClient client,
        FakeRepository repository,
        FakeUnitOfWork unitOfWork) =>
        new(client, repository, unitOfWork, new IncrementingTimeProvider(), NullLogger<BlockchainService>.Instance);

    private sealed class FakeBlockCypherClient(
        Func<BlockchainNetwork, CancellationToken, Task<BlockchainData>> getLatestBlock) : IBlockCypherClient
    {
        public Task<BlockchainData> GetLatestBlockAsync(BlockchainNetwork network, CancellationToken cancellationToken) =>
            getLatestBlock(network, cancellationToken);
    }

    private sealed class FakeRepository : IBlockchainSnapshotRepository
    {
        public List<BlockchainSnapshot> AddedSnapshots { get; } = [];

        public Task AddAsync(BlockchainSnapshot snapshot, CancellationToken cancellationToken)
        {
            AddedSnapshots.Add(snapshot);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<BlockchainSnapshot>> GetHistoryAsync(
            BlockchainNetwork network,
            DateTimeOffset from,
            DateTimeOffset to,
            int limit,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BlockchainSnapshot>>([]);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed class IncrementingTimeProvider : TimeProvider
    {
        private long _ticks;
        private readonly DateTimeOffset _start = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => _start.AddTicks(Interlocked.Increment(ref _ticks));
    }
}
