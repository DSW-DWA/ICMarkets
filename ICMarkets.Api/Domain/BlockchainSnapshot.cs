namespace ICMarkets.Api.Domain;

public sealed class BlockchainSnapshot
{
    private BlockchainSnapshot() { }

    public BlockchainSnapshot(BlockchainNetwork network, BlockchainData data, DateTimeOffset createdAt)
    {
        Network = network;
        Data = data;
        CreatedAt = createdAt.UtcDateTime;
    }

    public long Id { get; private set; }
    public BlockchainNetwork Network { get; private set; }
    public BlockchainData Data { get; private set; } = new();
    public DateTime CreatedAt { get; private set; }
}
