using Bruktborsen.Api.DTOs.Analytics;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repository.Interfaces;
using Bruktborsen.Api.Services.Interfaces;

namespace Bruktborsen.Api.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsRepository _repository;

    public AnalyticsService(IAnalyticsRepository repository)
    {
        _repository = repository;
    }

    public async Task<PriceSuggestionDto?> GetPriceSuggestionAsync(
        int categoryId,
        Condition condition)
    {
        var category = await _repository.GetCategoryByIdAsync(
            categoryId);

        if (category is null)
        {
            return null;
        }

        var soldListings = await _repository
            .GetSoldListingsByCategoryAsync(categoryId);

        if (soldListings.Count < 3)
        {
            return new PriceSuggestionDto(
                categoryId,
                condition,
                soldListings.Count,
                null,
                "At least three sold listings are required."
            );
        }

        var normalizedPrices = soldListings.Select(
            listing => AnalyticsMath.NormalizePrice(
                listing.SoldPrice!.Value,
                listing.Condition
            )
        );

        decimal normalizedMedian = AnalyticsMath.Median(
            normalizedPrices);

        decimal suggestedPrice = AnalyticsMath.RoundToNearestTen(
            normalizedMedian *
            AnalyticsMath.GetConditionFactor(condition)
        );

        return new PriceSuggestionDto(
            categoryId,
            condition,
            soldListings.Count,
            suggestedPrice,
            "Price suggestion calculated successfully."
        );
    }

    public async Task<PriceAssessmentDto?> GetPriceAssessmentAsync(
        int listingId)
    {
        var listing = await _repository.GetListingByIdAsync(
            listingId);

        if (listing is null)
        {
            return null;
        }

        var soldListings = await _repository
            .GetSoldListingsByCategoryAsync(listing.CategoryId);

        // Normalize historical sales to the listing's condition.
        var comparablePrices = soldListings.Select(
            sold => AnalyticsMath.NormalizePrice(
                sold.SoldPrice!.Value,
                sold.Condition
            ) * AnalyticsMath.GetConditionFactor(listing.Condition)
        ).ToArray();

        if (comparablePrices.Length < 5)
        {
            return new PriceAssessmentDto(
                listingId,
                "Unknown",
                comparablePrices.Length,
                null,
                null
            );
        }

        decimal q1 = AnalyticsMath.Quartile1(comparablePrices);
        decimal q3 = AnalyticsMath.Quartile3(comparablePrices);
        decimal iqr = q3 - q1;

        decimal lowerFence = q1 - 1.5m * iqr;
        decimal upperFence = q3 + 1.5m * iqr;

        string assessment = AnalyticsMath.AssessPrice(
            listing.Price,
            comparablePrices);

        return new PriceAssessmentDto(
            listingId,
            assessment,
            comparablePrices.Length,
            lowerFence,
            upperFence
        );
    }

    public async Task<CategoryStatsDto?> GetCategoryStatsAsync(
        int categoryId)
    {
        var category = await _repository.GetCategoryByIdAsync(
            categoryId);

        if (category is null)
        {
            return null;
        }

        var listings = await _repository.GetAllListingsAsync();

        return CalculateCategoryStats(
            category,
            listings.Where(l => l.CategoryId == categoryId)
        );
    }

    public async Task<AnalyticsOverviewDto> GetOverviewAsync()
    {
        var categories = await _repository.GetCategoriesAsync();
        var listings = await _repository.GetAllListingsAsync();

        int activeCount = listings.Count(
            l => l.Status == ListingStatus.Active);

        int soldCount = listings.Count(
            l => l.Status == ListingStatus.Sold);

        int removedCount = listings.Count(
            l => l.Status == ListingStatus.Removed);

        var soldPrices = listings
            .Where(l =>
                l.Status == ListingStatus.Sold &&
                l.SoldPrice.HasValue)
            .Select(l => (decimal)l.SoldPrice!.Value)
            .ToArray();

        decimal? averageSoldPrice = soldPrices.Length > 0
            ? soldPrices.Average()
            : null;

        decimal soldFraction = listings.Count > 0
            ? (decimal)soldCount / listings.Count
            : 0m;

        var categoryStats = categories.Select(
            category => CalculateCategoryStats(
                category,
                listings.Where(l => l.CategoryId == category.Id)
            )
        ).ToList();

        return new AnalyticsOverviewDto(
            listings.Count,
            activeCount,
            soldCount,
            removedCount,
            averageSoldPrice,
            soldFraction,
            categoryStats
        );
    }

    public async Task<List<PriceHistoryDto>?> GetPriceHistoryAsync(
        int listingId)
    {
        var listing = await _repository.GetListingByIdAsync(
            listingId);

        if (listing is null)
        {
            return null;
        }

        var history = await _repository.GetPriceHistoryAsync(
            listingId);

        return history.Select(change => new PriceHistoryDto(
            change.Id,
            change.ListingId,
            change.OldPrice,
            change.NewPrice,
            change.ChangedDate
        )).ToList();
    }

    private static CategoryStatsDto CalculateCategoryStats(
        Category category,
        IEnumerable<Listing> categoryListings)
    {
        var listings = categoryListings.ToArray();

        int activeCount = listings.Count(
            l => l.Status == ListingStatus.Active);

        int soldCount = listings.Count(
            l => l.Status == ListingStatus.Sold);

        int removedCount = listings.Count(
            l => l.Status == ListingStatus.Removed);

        var soldListings = listings
            .Where(l =>
                l.Status == ListingStatus.Sold &&
                l.SoldPrice.HasValue)
            .ToArray();

        var soldPrices = soldListings
            .Select(l => (decimal)l.SoldPrice!.Value)
            .ToArray();

        decimal? averagePrice = soldPrices.Length > 0
            ? soldPrices.Average()
            : null;

        decimal? medianPrice = soldPrices.Length > 0
            ? AnalyticsMath.Median(soldPrices)
            : null;

        decimal? standardDeviation = soldPrices.Length >= 2
            ? AnalyticsMath.SampleStandardDeviation(soldPrices)
            : null;

        var saleTimes = soldListings
            .Where(l => l.SoldDate.HasValue)
            .Select(l =>
                (l.SoldDate!.Value - l.CreatedDate).TotalDays)
            .ToArray();

        double? averageDaysToSell = saleTimes.Length > 0
            ? saleTimes.Average()
            : null;

        var discountPercentages = soldListings
            .Select(l =>
            {
                var firstPriceChange = l.PriceChanges
                    .OrderBy(p => p.ChangedDate)
                    .ThenBy(p => p.Id)
                    .FirstOrDefault();

                decimal originalPrice = firstPriceChange is null
                    ? l.Price
                    : firstPriceChange.OldPrice;

                if (originalPrice <= 0)
                {
                    return (decimal?)null;
                }

                decimal discount =
                    (originalPrice - l.SoldPrice!.Value) /
                    originalPrice * 100m;

                return (decimal?)discount;
            })
            .Where(discount => discount.HasValue)
            .Select(discount => discount!.Value)
            .ToArray();

        decimal? averageDiscount = discountPercentages.Length > 0
            ? discountPercentages.Average()
            : null;

        decimal soldFraction = listings.Length > 0
            ? (decimal)soldCount / listings.Length
            : 0m;

        return new CategoryStatsDto(
            category.Id,
            category.Name,
            activeCount,
            soldCount,
            removedCount,
            averagePrice,
            medianPrice,
            standardDeviation,
            averageDaysToSell,
            averageDiscount,
            soldFraction
        );
    }
}
