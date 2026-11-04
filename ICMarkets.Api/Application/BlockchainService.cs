using ICMarkets.Api.Domain;

namespace ICMarkets.Api.Application;

public sealed class BlockchainService(
    IBlockCypherClient client,
    IBlockchainSnapshotRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<BlockchainService> logger) : IBlockchainService
{
    public async Task<BlockchainSnapshot> CollectAsync(BlockchainNetwork network, CancellationToken cancellationToken)
    {
        var requestedAt = timeProvider.GetUtcNow();
        var data = await client.GetLatestBlockAsync(network, cancellationToken);
        var snapshot = new BlockchainSnapshot(network, data, requestedAt);
        await repository.AddAsync(snapshot, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return snapshot;
    }

    public async Task<IReadOnlyList<BlockchainCollectionOutcome>> CollectAllAsync(CancellationToken cancellationToken)
    {
        var networks = Enum.GetValues<BlockchainNetwork>();
        var outcomes = await Task.WhenAll(networks.Select(network => CollectNetworkAsync(network, cancellationToken)));
        var successfulSnapshots = outcomes
            .Where(outcome => outcome.Snapshot is not null)
            .Select(outcome => outcome.Snapshot!)
            .ToArray();

        foreach (var snapshot in successfulSnapshots)
            await repository.AddAsync(snapshot, cancellationToken);

        if (successfulSnapshots.Length > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        return outcomes;
    }

    public Task<IReadOnlyList<BlockchainSnapshot>> GetHistoryAsync(BlockchainNetwork network, DateTimeOffset from, DateTimeOffset to, int limit, CancellationToken cancellationToken) =>
        repository.GetHistoryAsync(network, from, to, limit, cancellationToken);

    private async Task<BlockchainCollectionOutcome> CollectNetworkAsync(BlockchainNetwork network, CancellationToken cancellationToken)
    {
        var requestedAt = timeProvider.GetUtcNow();
        try
        {
            var data = await client.GetLatestBlockAsync(network, cancellationToken);
            return new BlockchainCollectionOutcome(network, new BlockchainSnapshot(network, data, requestedAt), null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Collecting blockchain data failed for {Network}.", network);
            return new BlockchainCollectionOutcome(network, null, "Failed to collect data for this network.");
        }
    }
}
