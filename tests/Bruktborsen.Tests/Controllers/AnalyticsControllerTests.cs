using Bruktborsen.Api.Controllers;
using Bruktborsen.Api.DTOs.Analytics;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bruktborsen.Tests.Controllers;

public class AnalyticsControllerTests
{
    [Fact]
    public async Task GetPriceSuggestion_InvalidCategoryId_Returns400()
    {
        // Arrange
        var controller = new AnalyticsController(
            new FakeAnalyticsService()
        );

        // Act
        var result = await controller.GetPriceSuggestion(
            0,
            Condition.Good
        );

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetCategoryStats_MissingCategory_Returns404()
    {
        // Arrange
        var controller = new AnalyticsController(
            new FakeAnalyticsService()
        );

        // Act
        var result = await controller.GetCategoryStats(99999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetOverview_Returns200WithStatistics()
    {
        // Arrange
        var controller = new AnalyticsController(
            new FakeAnalyticsService()
        );

        // Act
        var result = await controller.GetOverview();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(
            result.Result
        );

        Assert.Equal(200, okResult.StatusCode);

        var overview = Assert.IsType<AnalyticsOverviewDto>(
            okResult.Value
        );

        Assert.Equal(3, overview.TotalListings);
        Assert.Equal(1, overview.SoldListings);
    }

    private sealed class FakeAnalyticsService : IAnalyticsService
    {
        public Task<PriceSuggestionDto?> GetPriceSuggestionAsync(
            int categoryId,
            Condition condition)
        {
            return Task.FromResult<PriceSuggestionDto?>(null);
        }

        public Task<PriceAssessmentDto?> GetPriceAssessmentAsync(
            int listingId)
        {
            return Task.FromResult<PriceAssessmentDto?>(null);
        }

        public Task<CategoryStatsDto?> GetCategoryStatsAsync(
            int categoryId)
        {
            return Task.FromResult<CategoryStatsDto?>(null);
        }

        public Task<AnalyticsOverviewDto> GetOverviewAsync()
        {
            var overview = new AnalyticsOverviewDto(
                TotalListings: 3,
                ActiveListings: 1,
                SoldListings: 1,
                RemovedListings: 1,
                AverageSoldPrice: 8000m,
                SoldFraction: 1m / 3m,
                Categories: new List<CategoryStatsDto>()
            );

            return Task.FromResult(overview);
        }

        public Task<List<PriceHistoryDto>?> GetPriceHistoryAsync(
            int listingId)
        {
            return Task.FromResult<List<PriceHistoryDto>?>(
                null
            );
        }
    }
}
