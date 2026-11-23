using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.DTOs.Offers;

public sealed record OfferDetailDto(
    int Id,
    int ListingId,
    string ListingTitle,
    string BuyerId,
    string BuyerName,
    int Amount,
    OfferStatus Status,
    DateTime CreatedDate
);
