namespace Bruktborsen.Api.DTOs.Analytics;

public sealed record PriceHistoryDto(
    int Id,
    int ListingId,
    int OldPrice,
    int NewPrice,
    DateTime ChangedDate
);
