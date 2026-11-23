using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.Repository.Interfaces;

public interface IListingRepository
{
    Task<List<Listing>> GetActiveAsync(
        string? search,
        int? categoryId,
        Condition? condition,
        int? minPrice,
        int? maxPrice,
        string? sort);

    Task<Listing?> GetByIdAsync(int id);

    Task<List<Listing>> GetBySellerAsync(string sellerId);

    Task<bool> CategoryExistsAsync(int categoryId);

    Task<Listing> AddAsync(Listing listing);

    Task UpdateAsync(Listing listing);

    Task UpdatePriceAsync(Listing listing, PriceChange priceChange);
}
