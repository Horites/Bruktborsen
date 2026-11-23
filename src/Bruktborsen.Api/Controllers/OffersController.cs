using System.Security.Claims;
using Bruktborsen.Api.DTOs.Offers;
using Bruktborsen.Api.Services;
using Bruktborsen.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bruktborsen.Api.Controllers;

[ApiController]
[Authorize]
public class OffersController : ControllerBase
{
    private readonly IOfferService _offerService;

    public OffersController(IOfferService offerService)
    {
        _offerService = offerService;
    }

    // GET /api/listings/{id}/offers
    [HttpGet("api/listings/{id:int}/offers")]
    [ProducesResponseType(
        typeof(List<OfferDetailDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<OfferDetailDto>>>
        GetByListing(int id)
    {
        var userId = GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _offerService.GetByListingAsync(
            id,
            userId);

        return result.Result switch
        {
            OfferResult.Success when result.Data is not null =>
                Ok(result.Data),

            OfferResult.NotFound => NotFound(),

            OfferResult.Forbidden => Forbid(),

            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    // POST /api/listings/{id}/offers
    [HttpPost("api/listings/{id:int}/offers")]
    [ProducesResponseType(
        typeof(OfferDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OfferDto>> Create(
        int id,
        [FromBody] CreateOfferDto dto)
    {
        var userId = GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _offerService.CreateAsync(
            id,
            dto,
            userId);

        return result.Result switch
        {
            OfferResult.Success when result.Data is not null =>
                StatusCode(
                    StatusCodes.Status201Created,
                    result.Data),

            OfferResult.NotFound => NotFound(),

            OfferResult.OwnListing or
            OfferResult.InvalidAmount or
            OfferResult.AmountTooLow => BadRequest(new
            {
                message = result.Message
            }),

            OfferResult.ListingNotActive or
            OfferResult.DuplicatePending => Conflict(new
            {
                message = result.Message
            }),

            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    // GET /api/offers/my
    [HttpGet("api/offers/my")]
    [ProducesResponseType(
        typeof(List<OfferDetailDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<OfferDetailDto>>> GetMy()
    {
        var userId = GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var offers = await _offerService.GetMyOffersAsync(
            userId);

        return Ok(offers);
    }

    // POST /api/offers/{id}/accept
    [HttpPost("api/offers/{id:int}/accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Accept(int id)
    {
        var userId = GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _offerService.AcceptAsync(id, userId);

        return MapMutationResult(result);
    }

    // POST /api/offers/{id}/reject
    [HttpPost("api/offers/{id:int}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(int id)
    {
        var userId = GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _offerService.RejectAsync(id, userId);

        return MapMutationResult(result);
    }

    // POST /api/offers/{id}/withdraw
    [HttpPost("api/offers/{id:int}/withdraw")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Withdraw(int id)
    {
        var userId = GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _offerService.WithdrawAsync(id, userId);

        return MapMutationResult(result);
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private IActionResult MapMutationResult(OfferResult result)
    {
        return result switch
        {
            OfferResult.Success => NoContent(),

            OfferResult.NotFound => NotFound(),

            OfferResult.Forbidden => Forbid(),

            OfferResult.ListingNotActive or
            OfferResult.DuplicatePending or
            OfferResult.OfferNotPending => Conflict(new
            {
                message = "Offer cannot be modified in its current state."
            }),

            OfferResult.OwnListing or
            OfferResult.AmountTooLow or
            OfferResult.InvalidAmount => BadRequest(),

            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
