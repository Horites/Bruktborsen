using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.DTOs.Listings;

public sealed record ListingDto(
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
    string SellerId
);
