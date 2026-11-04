using ICMarkets.Api.Application;
using ICMarkets.Api.Configuration;
using ICMarkets.Api.Domain;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ICMarkets.Api.Infrastructure;

public sealed class BlockCypherClient(HttpClient httpClient, IOptions<BlockCypherSettings> options) : IBlockCypherClient
{
    private readonly BlockCypherSettings _settings = options.Value;

    public async Task<BlockchainData> GetLatestBlockAsync(BlockchainNetwork network, CancellationToken cancellationToken)
    {
        if (!_settings.Networks.TryGetValue(network.ToString(), out var path))
            throw new ArgumentOutOfRangeException(nameof(network), network, "Unsupported blockchain network.");

        using var response = await httpClient.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        try
        {
            return await response.Content.ReadFromJsonAsync<BlockchainData>(cancellationToken: cancellationToken)
                ?? throw new InvalidUpstreamResponseException("BlockCypher returned an empty response.");
        }
        catch (JsonException exception)
        {
            throw new InvalidUpstreamResponseException("BlockCypher returned an invalid response.", exception);
        }
    }
}
