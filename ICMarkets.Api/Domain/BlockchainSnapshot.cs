namespace ICMarkets.Api.Domain;

public sealed class BlockchainSnapshot
{
    private BlockchainSnapshot() { }

    public BlockchainSnapshot(BlockchainNetwork network, string responseJson, DateTimeOffset createdAt)
    {
        Network = network;
        ResponseJson = responseJson;
        CreatedAt = createdAt.UtcDateTime;
    }

    public long Id { get; private set; }
    public BlockchainNetwork Network { get; private set; }
    public string ResponseJson { get; private set; } = "{}";
    public DateTime CreatedAt { get; private set; }
}
