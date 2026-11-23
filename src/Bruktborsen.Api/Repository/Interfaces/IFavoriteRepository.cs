using Bruktborsen.Api.Models;

namespace Bruktborsen.Api.Repository.Interfaces;

public interface IFavoriteRepository
{
    Task<List<Favorite>> GetByUserAsync(string userId);

    Task<Favorite?> GetAsync(int listingId, string userId);

    Task<bool> ListingExistsAsync(int listingId);

    Task AddAsync(Favorite favorite);

    Task DeleteAsync(Favorite favorite);
}
