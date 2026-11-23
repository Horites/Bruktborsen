using System.Security.Claims;
using Bruktborsen.Api.DTOs.Favorites;
using Bruktborsen.Api.Services;
using Bruktborsen.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bruktborsen.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/favorites")]
public class FavoritesController : ControllerBase
{
    private readonly IFavoriteService _favoriteService;

    public FavoritesController(IFavoriteService favoriteService)
    {
        _favoriteService = favoriteService;
    }

    // GET /api/favorites
    [HttpGet]
    [ProducesResponseType(
        typeof(List<FavoriteDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<FavoriteDto>>> GetMy()
    {
        var userId = GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var favorites = await _favoriteService
            .GetMyFavoritesAsync(userId);

        return Ok(favorites);
    }

    // POST /api/favorites/{listingId}
    [HttpPost("{listingId:int}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Add(int listingId)
    {
        var userId = GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _favoriteService.AddAsync(
            listingId,
            userId);

        return result switch
        {
            FavoriteResult.Success => StatusCode(
                StatusCodes.Status201Created),

            FavoriteResult.NotFound => NotFound(),

            FavoriteResult.AlreadyExists => Conflict(new
            {
                message = "Listing is already in your favorites."
            }),

            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    // DELETE /api/favorites/{listingId}
    [HttpDelete("{listingId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(int listingId)
    {
        var userId = GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _favoriteService.RemoveAsync(
            listingId,
            userId);

        return result switch
        {
            FavoriteResult.Success => NoContent(),

            FavoriteResult.NotFound => NotFound(),

            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
