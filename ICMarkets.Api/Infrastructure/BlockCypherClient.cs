using ICMarkets.Api.Application;
using ICMarkets.Api.Configuration;
using ICMarkets.Api.Domain;
using Microsoft.Extensions.Options;

namespace ICMarkets.Api.Infrastructure;

public sealed class BlockCypherClient(HttpClient httpClient, IOptions<BlockCypherSettings> options) : IBlockCypherClient
{
    private readonly BlockCypherSettings _settings = options.Value;

    public async Task<string> GetLatestBlockAsync(BlockchainNetwork network, CancellationToken cancellationToken)
    {
        if (!_settings.Networks.TryGetValue(network.ToString(), out var path))
            throw new ArgumentOutOfRangeException(nameof(network), network, "Unsupported blockchain network.");

        using var response = await httpClient.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
