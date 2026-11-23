using System.Security.Claims;
using Bruktborsen.Api.DTOs.Listings;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Services;
using Bruktborsen.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bruktborsen.Api.Controllers;

[ApiController]
[Route("api/listings")]
public class ListingsController : ControllerBase
{
    private readonly IListingService _listingService;
    private readonly IAnalyticsService _analyticsService;

    public ListingsController(
        IListingService listingService,
        IAnalyticsService analyticsService
        )
    {
        _listingService = listingService;
        _analyticsService = analyticsService;
    }

    // GET /api/listings
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(List<ListingDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ListingDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] Condition? condition,
        [FromQuery] int? minPrice,
        [FromQuery] int? maxPrice,
        [FromQuery] string? sort)
    {
        if (minPrice < 0 ||
            maxPrice < 0 ||
            (minPrice.HasValue &&
             maxPrice.HasValue &&
             minPrice > maxPrice))
        {
            return BadRequest(new
            {
                message = "Invalid price range."
            });
        }

        if (condition.HasValue &&
            !Enum.IsDefined(condition.Value))
        {
            return BadRequest(new
            {
                message = "Invalid condition."
            });
        }

        if (!string.IsNullOrWhiteSpace(sort) &&
            sort is not ("newest" or "price_asc" or "price_desc"))
        {
            return BadRequest(new
            {
                message = "Invalid sort value."
            });
        }

        var listings = await _listingService.GetActiveAsync(
            search,
            categoryId,
            condition,
            minPrice,
            maxPrice,
            sort
        );

        return Ok(listings);
    }

    // GET /api/listings/my
    // Must be declared before GET /api/listings/{id:int}
    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType(
        typeof(List<ListingDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ListingDto>>> GetMy()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var listings = await _listingService.GetMyListingsAsync(
            userId);

        return Ok(listings);
    }

    // GET /api/listings/{id}
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ListingDetailDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ListingDetailDto>> GetById(
    int id)
    {
        var listing = await _listingService.GetByIdAsync(id);

        if (listing is null)
        {
            return NotFound();
        }

        var assessment = await _analyticsService
            .GetPriceAssessmentAsync(id);

        var updatedListing = listing with
        {
            PriceAssessment = assessment?.Assessment ?? "Unknown"
        };

        return Ok(updatedListing);
    }

    // POST /api/listings
    [HttpPost]
    [Authorize]
    [ProducesResponseType(
        typeof(ListingDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ListingDto>> Create(
        [FromBody] CreateListingDto dto)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _listingService.CreateAsync(
            dto,
            userId);

        return result.Result switch
        {
            ListingResult.Success when result.Data is not null =>
                CreatedAtAction(
                    nameof(GetById),
                    new { id = result.Data.Id },
                    result.Data),

            ListingResult.InvalidData =>
                BadRequest(new
                {
                    message = result.Message
                }),

            ListingResult.CategoryNotFound => NotFound(
                new
                {
                    message = result.Message
                }),

            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    // PUT /api/listings/{id}
    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateListingDto dto)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var isAdmin = User.IsInRole("Admin");

        var result = await _listingService.UpdateAsync(
            id,
            dto,
            userId,
            isAdmin);

        return MapMutationResult(result);
    }

    // DELETE /api/listings/{id}
    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remove(int id)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var isAdmin = User.IsInRole("Admin");

        var result = await _listingService.RemoveAsync(
            id,
            userId,
            isAdmin);

        return MapMutationResult(result);
    }

    private IActionResult MapMutationResult(ListingResult result)
    {
        return result switch
        {
            ListingResult.Success => NoContent(),

            ListingResult.NotFound or
            ListingResult.CategoryNotFound => NotFound(),

            ListingResult.InvalidData => BadRequest(),

            ListingResult.Forbidden => Forbid(),

            ListingResult.Sold or
            ListingResult.Removed or
            ListingResult.NotActive => Conflict(new
            {
                message = "Listing cannot be modified in its current state."
            }),

            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
