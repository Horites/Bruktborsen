using Bruktborsen.Api.DTOs.Listings;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repository.Interfaces;
using Bruktborsen.Api.Services.Interfaces;

namespace Bruktborsen.Api.Services;

public class ListingService : IListingService
{
    private readonly IListingRepository _repository;

    public ListingService(IListingRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<ListingDto>> GetActiveAsync(
        string? search,
        int? categoryId,
        Condition? condition,
        int? minPrice,
        int? maxPrice,
        string? sort)
    {
        var listings = await _repository.GetActiveAsync(
            search,
            categoryId,
            condition,
            minPrice,
            maxPrice,
            sort
        );

        return listings.Select(ToDto).ToList();
    }

    public async Task<ListingDetailDto?> GetByIdAsync(int id)
    {
        var listing = await _repository.GetByIdAsync(id);

        if (listing is null)
        {
            return null;
        }

        // TODO BB-008:
        // Replace Unknown with AnalyticsService assessment.
        var priceAssessment = "Unknown";

        return new ListingDetailDto(
            listing.Id,
            listing.Title,
            listing.Description,
            listing.Price,
            listing.Condition,
            listing.Status,
            listing.ImageUrl,
            listing.CreatedDate,
            listing.SoldDate,
            listing.SoldPrice,
            listing.CategoryId,
            listing.Category.Name,
            listing.SellerId,
            listing.Seller.FullName,
            listing.Offers.Count,
            priceAssessment
        );
    }

    public async Task<List<ListingDto>> GetMyListingsAsync(
        string sellerId)
    {
        var listings = await _repository.GetBySellerAsync(
            sellerId);

        return listings.Select(ToDto).ToList();
    }

    public async Task<ListingOperationResult<ListingDto>>
        CreateAsync(
            CreateListingDto dto,
            string sellerId)
    {
        if (!IsValid(
                dto.Title,
                dto.Description,
                dto.Price,
                dto.Condition))
        {
            return new(
                ListingResult.InvalidData,
                Message: "Invalid listing data."
            );
        }

        if (dto.CategoryId <= 0 ||
            !await _repository.CategoryExistsAsync(
                dto.CategoryId))
        {
            return new(
                ListingResult.CategoryNotFound,
                Message: "Category not found."
            );
        }

        if (string.IsNullOrWhiteSpace(sellerId))
        {
            return new(
                ListingResult.InvalidData,
                Message: "Seller is required."
            );
        }

        var listing = new Listing
        {
            Title = dto.Title.Trim(),
            Description = dto.Description,
            Price = dto.Price,
            Condition = dto.Condition,
            Status = ListingStatus.Active,
            ImageUrl = dto.ImageUrl,
            CreatedDate = DateTime.UtcNow,
            CategoryId = dto.CategoryId,
            SellerId = sellerId
        };

        var created = await _repository.AddAsync(listing);

        // Reload to include Category navigation properties.
        var saved = await _repository.GetByIdAsync(created.Id);

        if (saved is null)
        {
            throw new InvalidOperationException(
                "Created listing could not be retrieved."
            );
        }

        return new(
            ListingResult.Success,
            ToDto(saved)
        );
    }

    public async Task<ListingResult> UpdateAsync(
        int id,
        UpdateListingDto dto,
        string userId,
        bool isAdmin)
    {
        var listing = await _repository.GetByIdAsync(id);

        if (listing is null)
        {
            return ListingResult.NotFound;
        }

        if (!CanManage(listing, userId, isAdmin))
        {
            return ListingResult.Forbidden;
        }

        if (listing.Status == ListingStatus.Sold)
        {
            return ListingResult.Sold;
        }

        if (listing.Status == ListingStatus.Removed)
        {
            return ListingResult.Removed;
        }

        if (!IsValid(
                dto.Title,
                dto.Description,
                dto.Price,
                dto.Condition))
        {
            return ListingResult.InvalidData;
        }

        if (dto.CategoryId <= 0 ||
            !await _repository.CategoryExistsAsync(
                dto.CategoryId))
        {
            return ListingResult.CategoryNotFound;
        }

        var oldPrice = listing.Price;
        var priceChanged = oldPrice != dto.Price;

        listing.Title = dto.Title.Trim();
        listing.Description = dto.Description;
        listing.Price = dto.Price;
        listing.Condition = dto.Condition;
        listing.ImageUrl = dto.ImageUrl;
        listing.CategoryId = dto.CategoryId;

        if (priceChanged)
        {
            var priceChange = new PriceChange
            {
                ListingId = listing.Id,
                OldPrice = oldPrice,
                NewPrice = dto.Price,
                ChangedDate = DateTime.UtcNow
            };

            await _repository.UpdatePriceAsync(
                listing,
                priceChange
            );
        }
        else
        {
            await _repository.UpdateAsync(listing);
        }

        return ListingResult.Success;
    }

    public async Task<ListingResult> RemoveAsync(
        int id,
        string userId,
        bool isAdmin)
    {
        var listing = await _repository.GetByIdAsync(id);

        if (listing is null)
        {
            return ListingResult.NotFound;
        }

        if (!CanManage(listing, userId, isAdmin))
        {
            return ListingResult.Forbidden;
        }

        if (listing.Status == ListingStatus.Sold)
        {
            return ListingResult.Sold;
        }

        if (listing.Status == ListingStatus.Removed)
        {
            return ListingResult.Removed;
        }

        listing.Status = ListingStatus.Removed;

        await _repository.UpdateAsync(listing);

        return ListingResult.Success;
    }

    private static bool CanManage(
        Listing listing,
        string userId,
        bool isAdmin)
    {
        return isAdmin || listing.SellerId == userId;
    }

    private static bool IsValid(
        string? title,
        string? description,
        int price,
        Condition condition)
    {
        return !string.IsNullOrWhiteSpace(title)
            && title.Trim().Length <= 100
            && (description is null ||
                description.Length <= 2000)
            && price > 0
            && Enum.IsDefined(condition);
    }

    private static ListingDto ToDto(Listing listing)
    {
        return new ListingDto(
            listing.Id,
            listing.Title,
            listing.Description,
            listing.Price,
            listing.Condition,
            listing.Status,
            listing.ImageUrl,
            listing.CreatedDate,
            listing.SoldDate,
            listing.SoldPrice,
            listing.CategoryId,
            listing.Category.Name,
            listing.SellerId
        );
    }
}
