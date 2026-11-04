using ICMarkets.Api.Domain;

namespace ICMarkets.Api.Application;

public interface IBlockchainSnapshotRepository
{
    Task AddAsync(BlockchainSnapshot snapshot, CancellationToken cancellationToken);
    Task<IReadOnlyList<BlockchainSnapshot>> GetHistoryAsync(BlockchainNetwork network, DateTimeOffset from, DateTimeOffset to, int limit, CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IBlockCypherClient
{
    Task<BlockchainData> GetLatestBlockAsync(BlockchainNetwork network, CancellationToken cancellationToken);
}

public sealed class InvalidUpstreamResponseException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public interface IBlockchainService
{
    Task<BlockchainSnapshot> CollectAsync(BlockchainNetwork network, CancellationToken cancellationToken);
    Task<IReadOnlyList<BlockchainCollectionOutcome>> CollectAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<BlockchainSnapshot>> GetHistoryAsync(BlockchainNetwork network, DateTimeOffset from, DateTimeOffset to, int limit, CancellationToken cancellationToken);
}

public sealed record BlockchainCollectionOutcome(
    BlockchainNetwork Network,
    BlockchainSnapshot? Snapshot,
    string? Error)
{
    public bool Succeeded => Snapshot is not null;
}
