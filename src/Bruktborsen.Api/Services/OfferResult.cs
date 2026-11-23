namespace Bruktborsen.Api.Services;

public enum OfferResult
{
    Success,
    NotFound,
    Forbidden,
    OwnListing,
    ListingNotActive,
    DuplicatePending,
    AmountTooLow,
    InvalidAmount,
    OfferNotPending
}
