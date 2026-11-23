using Bruktborsen.Api.DTOs.Offers;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repository.Interfaces;
using Bruktborsen.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Api.Services;

public class OfferService : IOfferService
{
    private readonly IOfferRepository _offerRepository;
    private readonly IListingRepository _listingRepository;

    public OfferService(
        IOfferRepository offerRepository,
        IListingRepository listingRepository)
    {
        _offerRepository = offerRepository;
        _listingRepository = listingRepository;
    }

    public async Task<OfferOperationResult<List<OfferDetailDto>>>
        GetByListingAsync(int listingId, string sellerId)
    {
        var listing = await _listingRepository.GetByIdAsync(listingId);

        if (listing is null)
        {
            return new(OfferResult.NotFound);
        }

        if (listing.SellerId != sellerId)
        {
            return new(OfferResult.Forbidden);
        }

        var offers = await _offerRepository.GetByListingAsync(listingId);

        return new(
            OfferResult.Success,
            offers.Select(ToDetailDto).ToList()
        );
    }

    public async Task<List<OfferDetailDto>> GetMyOffersAsync(
        string buyerId)
    {
        var offers = await _offerRepository.GetByBuyerAsync(buyerId);

        return offers.Select(ToDetailDto).ToList();
    }

    public async Task<OfferOperationResult<OfferDto>> CreateAsync(
        int listingId,
        CreateOfferDto dto,
        string buyerId)
    {
        var listing = await _listingRepository.GetByIdAsync(listingId);

        if (listing is null)
        {
            return new(OfferResult.NotFound);
        }

        if (listing.SellerId == buyerId)
        {
            return new(
                OfferResult.OwnListing,
                Message: "You cannot offer on your own listing."
            );
        }

        if (listing.Status != ListingStatus.Active)
        {
            return new(
                OfferResult.ListingNotActive,
                Message: "Listing is not active."
            );
        }

        if (dto.Amount <= 0)
        {
            return new(
                OfferResult.InvalidAmount,
                Message: "Offer amount must be positive."
            );
        }

        // Minimum 50%, including exactly 50%.
        // Avoid floating-point arithmetic.
        if ((long)dto.Amount * 2 < listing.Price)
        {
            return new(
                OfferResult.AmountTooLow,
                Message: "Offer must be at least 50% of listing price."
            );
        }

        if (await _offerRepository.HasPendingOfferAsync(
            listingId,
            buyerId))
        {
            return new(
                OfferResult.DuplicatePending,
                Message: "You already have a pending offer."
            );
        }

        var offer = new Offer
        {
            ListingId = listingId,
            BuyerId = buyerId,
            Amount = dto.Amount,
            Status = OfferStatus.Pending,
            CreatedDate = DateTime.UtcNow
        };

        try
        {
            var created = await _offerRepository.AddAsync(offer);

            return new(
                OfferResult.Success,
                ToDto(created)
            );
        }
        catch (DbUpdateException)
        {
            // The database also enforces a unique filtered index
            // for Pending offers.
            // A concurrent request may have created the same offer.
            if (await _offerRepository.HasPendingOfferAsync(
                listingId,
                buyerId))
            {
                return new(
                    OfferResult.DuplicatePending,
                    Message: "You already have a pending offer."
                );
            }

            throw;
        }
    }

    public async Task<OfferResult> AcceptAsync(
        int offerId,
        string sellerId)
    {
        var offer = await _offerRepository.GetByIdAsync(offerId);

        if (offer is null)
        {
            return OfferResult.NotFound;
        }

        if (offer.Listing.SellerId != sellerId)
        {
            return OfferResult.Forbidden;
        }

        if (offer.Listing.Status != ListingStatus.Active)
        {
            return OfferResult.ListingNotActive;
        }

        if (offer.Status != OfferStatus.Pending)
        {
            return OfferResult.OfferNotPending;
        }

        await _offerRepository.AcceptOfferAsync(offer);

        return OfferResult.Success;
    }

    public async Task<OfferResult> RejectAsync(
        int offerId,
        string sellerId)
    {
        var offer = await _offerRepository.GetByIdAsync(offerId);

        if (offer is null)
        {
            return OfferResult.NotFound;
        }

        if (offer.Listing.SellerId != sellerId)
        {
            return OfferResult.Forbidden;
        }

        if (offer.Status != OfferStatus.Pending)
        {
            return OfferResult.OfferNotPending;
        }

        offer.Status = OfferStatus.Rejected;

        await _offerRepository.UpdateAsync();

        return OfferResult.Success;
    }

    public async Task<OfferResult> WithdrawAsync(
        int offerId,
        string buyerId)
    {
        var offer = await _offerRepository.GetByIdAsync(offerId);

        if (offer is null)
        {
            return OfferResult.NotFound;
        }

        if (offer.BuyerId != buyerId)
        {
            return OfferResult.Forbidden;
        }

        if (offer.Status != OfferStatus.Pending)
        {
            return OfferResult.OfferNotPending;
        }

        offer.Status = OfferStatus.Withdrawn;

        await _offerRepository.UpdateAsync();

        return OfferResult.Success;
    }

    private static OfferDto ToDto(Offer offer)
    {
        return new OfferDto(
            offer.Id,
            offer.ListingId,
            offer.BuyerId,
            offer.Amount,
            offer.Status,
            offer.CreatedDate
        );
    }

    private static OfferDetailDto ToDetailDto(Offer offer)
    {
        return new OfferDetailDto(
            offer.Id,
            offer.ListingId,
            offer.Listing.Title,
            offer.BuyerId,
            offer.Buyer.FullName,
            offer.Amount,
            offer.Status,
            offer.CreatedDate
        );
    }
}
