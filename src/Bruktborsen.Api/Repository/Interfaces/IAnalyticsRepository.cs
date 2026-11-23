using Bruktborsen.Api.Models;

namespace Bruktborsen.Api.Repository.Interfaces;

public interface IAnalyticsRepository
{
    Task<Category?> GetCategoryByIdAsync(int categoryId);

    Task<List<Category>> GetCategoriesAsync();

    Task<List<Listing>> GetSoldListingsByCategoryAsync(
        int categoryId);

    Task<List<Listing>> GetAllListingsAsync();

    Task<Listing?> GetListingByIdAsync(int listingId);

    Task<List<PriceChange>> GetPriceHistoryAsync(
        int listingId);
}
