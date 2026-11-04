using System.ComponentModel.DataAnnotations;
using ICMarkets.Api.Domain;

namespace ICMarkets.Api.Contracts;

public sealed record SnapshotResponse(long Id, BlockchainNetwork Network, DateTimeOffset CreatedAt, BlockchainData Data);

public sealed record NetworkCollectionResponse(
    BlockchainNetwork Network,
    bool Succeeded,
    SnapshotResponse? Snapshot,
    string? Error);

public sealed record CollectAllResponse(
    IReadOnlyList<NetworkCollectionResponse> Results,
    int SuccessfulCount,
    int FailedCount);

public sealed class HistoryQuery : IValidatableObject
{
    [Range(1, 500)]
    public int Limit { get; init; } = 100;

    [Required]
    public DateTimeOffset? From { get; init; }

    [Required]
    public DateTimeOffset? To { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From >= To)
            yield return new ValidationResult("The 'from' value must be earlier than 'to'.", [nameof(From), nameof(To)]);
    }
}

public static class SnapshotMapping
{
    public static SnapshotResponse ToResponse(this BlockchainSnapshot snapshot)
    {
        return new(
            snapshot.Id,
            snapshot.Network,
            new DateTimeOffset(DateTime.SpecifyKind(snapshot.CreatedAt, DateTimeKind.Utc)),
            snapshot.Data);
    }
}
