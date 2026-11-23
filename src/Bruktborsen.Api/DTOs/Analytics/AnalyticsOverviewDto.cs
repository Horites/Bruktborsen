namespace Bruktborsen.Api.DTOs.Analytics;

public sealed record AnalyticsOverviewDto(
    int TotalListings,
    int ActiveListings,
    int SoldListings,
    int RemovedListings,
    decimal? AverageSoldPrice,
    decimal SoldFraction,
    List<CategoryStatsDto> Categories
);
