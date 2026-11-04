using System.Text.Json;
using System.Text.Json.Serialization;

namespace ICMarkets.Api.Domain;

/// <summary>Typed BlockCypher chain response. Extra fields are retained for chain-specific data.</summary>
public sealed class BlockchainData
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("height")]
    public long? Height { get; init; }

    [JsonPropertyName("hash")]
    public string? Hash { get; init; }

    [JsonPropertyName("time")]
    public string? Time { get; init; }

    [JsonPropertyName("latest_url")]
    public string? LatestUrl { get; init; }

    [JsonPropertyName("previous_hash")]
    public string? PreviousHash { get; init; }

    [JsonPropertyName("previous_url")]
    public string? PreviousUrl { get; init; }

    [JsonPropertyName("peer_count")]
    public int? PeerCount { get; init; }

    [JsonPropertyName("unconfirmed_count")]
    public long? UnconfirmedCount { get; init; }

    [JsonPropertyName("high_fee_per_kb")]
    public long? HighFeePerKb { get; init; }

    [JsonPropertyName("medium_fee_per_kb")]
    public long? MediumFeePerKb { get; init; }

    [JsonPropertyName("low_fee_per_kb")]
    public long? LowFeePerKb { get; init; }

    [JsonPropertyName("last_fork_height")]
    public long? LastForkHeight { get; init; }

    [JsonPropertyName("last_fork_hash")]
    public string? LastForkHash { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; init; }
}
