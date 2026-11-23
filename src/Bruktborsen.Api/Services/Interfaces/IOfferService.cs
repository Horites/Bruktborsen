using Bruktborsen.Api.DTOs.Offers;

namespace Bruktborsen.Api.Services.Interfaces;

public interface IOfferService
{
    Task<OfferOperationResult<List<OfferDetailDto>>>
        GetByListingAsync(
            int listingId,
            string sellerId
        );

    Task<List<OfferDetailDto>> GetMyOffersAsync(
        string buyerId
    );

    Task<OfferOperationResult<OfferDto>> CreateAsync(
        int listingId,
        CreateOfferDto dto,
        string buyerId
    );

    Task<OfferResult> AcceptAsync(
        int offerId,
        string sellerId
    );

    Task<OfferResult> RejectAsync(
        int offerId,
        string sellerId
    );

    Task<OfferResult> WithdrawAsync(
        int offerId,
        string buyerId
    );
}
