namespace Bruktborsen.Api.DTOs.Analytics;

public sealed record CategoryStatsDto(
    int CategoryId,
    string CategoryName,
    int ActiveCount,
    int SoldCount,
    int RemovedCount,
    decimal? AverageSoldPrice,
    decimal? MedianSoldPrice,
    decimal? SampleStandardDeviation,
    double? AverageDaysToSell,
    decimal? AverageDiscountPercent,
    decimal SoldFraction
);
