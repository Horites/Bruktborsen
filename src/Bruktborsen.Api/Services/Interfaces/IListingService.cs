using Bruktborsen.Api.DTOs.Listings;
using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.Services.Interfaces;

public interface IListingService
{
    Task<List<ListingDto>> GetActiveAsync(
        string? search,
        int? categoryId,
        Condition? condition,
        int? minPrice,
        int? maxPrice,
        string? sort
    );

    Task<ListingDetailDto?> GetByIdAsync(int id);

    Task<List<ListingDto>> GetMyListingsAsync(
        string sellerId
    );

    Task<ListingOperationResult<ListingDto>> CreateAsync(
        CreateListingDto dto,
        string sellerId
    );

    Task<ListingResult> UpdateAsync(
        int id,
        UpdateListingDto dto,
        string userId,
        bool isAdmin
    );

    Task<ListingResult> RemoveAsync(
        int id,
        string userId,
        bool isAdmin
    );
}
