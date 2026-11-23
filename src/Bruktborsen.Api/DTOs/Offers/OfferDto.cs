using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.DTOs.Offers;

public sealed record OfferDto(
    int Id,
    int ListingId,
    string BuyerId,
    int Amount,
    OfferStatus Status,
    DateTime CreatedDate
);
