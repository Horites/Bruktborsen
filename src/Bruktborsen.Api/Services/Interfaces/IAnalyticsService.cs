
using Bruktborsen.Api.DTOs.Analytics;
using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.Services.Interfaces;

public interface IAnalyticsService
{
    Task<PriceSuggestionDto?> GetPriceSuggestionAsync(
        int categoryId,
        Condition condition);

    Task<PriceAssessmentDto?> GetPriceAssessmentAsync(
        int listingId);

    Task<CategoryStatsDto?> GetCategoryStatsAsync(
        int categoryId);

    Task<AnalyticsOverviewDto> GetOverviewAsync();

    Task<List<PriceHistoryDto>?> GetPriceHistoryAsync(
        int listingId);
}
