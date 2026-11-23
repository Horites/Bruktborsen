using Bruktborsen.Api.DTOs.Analytics;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bruktborsen.Api.Controllers;

[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    // GET /api/analytics/price-suggestion
    [HttpGet("price-suggestion")]
    [Authorize]
    [ProducesResponseType(
        typeof(PriceSuggestionDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceSuggestionDto>> GetPriceSuggestion(
        [FromQuery] int categoryId,
        [FromQuery] Condition condition)
    {
        if (categoryId <= 0 ||
            !Enum.IsDefined(condition))
        {
            return BadRequest(new
            {
                message = "Invalid category or condition."
            });
        }

        var result = await _analyticsService
            .GetPriceSuggestionAsync(categoryId, condition);

        if (result is null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        return Ok(result);
    }

    // GET /api/analytics/listings/{id}/price-assessment
    [HttpGet("listings/{id:int}/price-assessment")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(PriceAssessmentDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceAssessmentDto>> GetPriceAssessment(
        int id)
    {
        var result = await _analyticsService
            .GetPriceAssessmentAsync(id);

        return result is null
            ? NotFound()
            : Ok(result);
    }

    // GET /api/analytics/categories/{id}/stats
    [HttpGet("categories/{id:int}/stats")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(CategoryStatsDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryStatsDto>> GetCategoryStats(
        int id)
    {
        var result = await _analyticsService
            .GetCategoryStatsAsync(id);

        return result is null
            ? NotFound()
            : Ok(result);
    }

    // GET /api/analytics/overview
    [HttpGet("overview")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(AnalyticsOverviewDto),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<AnalyticsOverviewDto>> GetOverview()
    {
        var result = await _analyticsService
            .GetOverviewAsync();

        return Ok(result);
    }

    // GET /api/analytics/listings/{id}/price-history
    [HttpGet("listings/{id:int}/price-history")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(List<PriceHistoryDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<PriceHistoryDto>>> GetPriceHistory(
        int id)
    {
        var result = await _analyticsService
            .GetPriceHistoryAsync(id);

        return result is null
            ? NotFound()
            : Ok(result);
    }
}
