using Bruktborsen.Api.DTOs.Favorites;

namespace Bruktborsen.Api.Services.Interfaces;

public interface IFavoriteService
{
    Task<List<FavoriteDto>> GetMyFavoritesAsync(string userId);

    Task<FavoriteResult> AddAsync(
        int listingId,
        string userId);

    Task<FavoriteResult> RemoveAsync(
        int listingId,
        string userId);
}
