using Bruktborsen.Api.DTOs.Favorites;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Repository.Interfaces;
using Bruktborsen.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Api.Services;

public class FavoriteService : IFavoriteService
{
    private readonly IFavoriteRepository _repository;

    public FavoriteService(IFavoriteRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<FavoriteDto>> GetMyFavoritesAsync(
        string userId)
    {
        var favorites = await _repository.GetByUserAsync(userId);

        return favorites.Select(f => new FavoriteDto(
            f.ListingId,
            f.Listing.Title,
            f.Listing.Price,
            f.Listing.ImageUrl,
            f.Listing.Category.Name
        )).ToList();
    }

    public async Task<FavoriteResult> AddAsync(
        int listingId,
        string userId)
    {
        if (!await _repository.ListingExistsAsync(listingId))
        {
            return FavoriteResult.NotFound;
        }

        if (await _repository.GetAsync(listingId, userId) is not null)
        {
            return FavoriteResult.AlreadyExists;
        }

        var favorite = new Favorite
        {
            ListingId = listingId,
            UserId = userId
        };

        try
        {
            await _repository.AddAsync(favorite);
        }
        catch (DbUpdateException)
        {
            // The unique database index protects against duplicates.
            // Verify the conflict before translating it.
            if (await _repository.GetAsync(listingId, userId) is not null)
            {
                return FavoriteResult.AlreadyExists;
            }

            throw;
        }

        return FavoriteResult.Success;
    }

    public async Task<FavoriteResult> RemoveAsync(
        int listingId,
        string userId)
    {
        var favorite = await _repository.GetAsync(
            listingId,
            userId);

        if (favorite is null)
        {
            return FavoriteResult.NotFound;
        }

        await _repository.DeleteAsync(favorite);

        return FavoriteResult.Success;
    }
}
