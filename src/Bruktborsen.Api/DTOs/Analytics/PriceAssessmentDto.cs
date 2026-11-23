namespace Bruktborsen.Api.DTOs.Analytics;

public sealed record PriceAssessmentDto(
    int ListingId,
    string Assessment,
    int ComparableSalesCount,
    decimal? LowerFence,
    decimal? UpperFence
);
