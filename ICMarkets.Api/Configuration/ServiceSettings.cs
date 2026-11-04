using System.ComponentModel.DataAnnotations;
using ICMarkets.Api.Domain;

namespace ICMarkets.Api.Configuration;

public sealed class BlockCypherSettings : IValidatableObject
{
    [Required]
    [Url]
    public string BaseUrl { get; init; } = "";

    [Required]
    [MinLength(1)]
    public Dictionary<string, string> Networks { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            yield return new ValidationResult("BaseUrl must be an absolute HTTP or HTTPS URL.", [nameof(BaseUrl)]);
        }

        if (Networks is null)
        {
            yield return new ValidationResult("Networks must be configured.", [nameof(Networks)]);
            yield break;
        }

        foreach (var network in Enum.GetNames<BlockchainNetwork>())
        {
            if (!Networks.TryGetValue(network, out var path) || string.IsNullOrWhiteSpace(path) || Uri.TryCreate(path, UriKind.Absolute, out _))
                yield return new ValidationResult($"Networks must contain a relative path for {network}.", [nameof(Networks)]);
        }
    }
}

public sealed class CollectionSettings : IValidatableObject
{
    public bool Enabled { get; init; } = true;
    public bool CollectOnStartup { get; init; } = true;
    public TimeSpan Interval { get; init; } = TimeSpan.FromMinutes(5);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Interval <= TimeSpan.Zero)
            yield return new ValidationResult("Interval must be greater than zero.", [nameof(Interval)]);
    }
}
