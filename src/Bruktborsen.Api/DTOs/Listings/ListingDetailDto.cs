using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.DTOs.Listings;

public sealed record ListingDetailDto(
    int Id,
    string Title,
    string? Description,
    int Price,
    Condition Condition,
    ListingStatus Status,
    string? ImageUrl,
    DateTime CreatedDate,
    DateTime? SoldDate,
    int? SoldPrice,
    int CategoryId,
    string CategoryName,
    string SellerId,
    string SellerName,
    int OfferCount,
    string PriceAssessment
);
