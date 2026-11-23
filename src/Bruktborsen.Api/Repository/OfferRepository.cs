using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Api.Repository;

public class OfferRepository : IOfferRepository
{
    private readonly ApplicationDbContext _context;

    public OfferRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Offer?> GetByIdAsync(int id)
    {
        return await _context.Offers
            .Include(o => o.Listing)
                .ThenInclude(l => l.Offers)
            .Include(o => o.Buyer)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<List<Offer>> GetByListingAsync(int listingId)
    {
        return await _context.Offers
            .AsNoTracking()
            .Include(o => o.Listing)
            .Include(o => o.Buyer)
            .Where(o => o.ListingId == listingId)
            .OrderByDescending(o => o.CreatedDate)
            .ToListAsync();
    }

    public async Task<List<Offer>> GetByBuyerAsync(
        string buyerId)
    {
        return await _context.Offers
            .AsNoTracking()
            .Include(o => o.Listing)
            .Include(o => o.Buyer)
            .Where(o => o.BuyerId == buyerId)
            .OrderByDescending(o => o.CreatedDate)
            .ToListAsync();
    }

    public async Task<bool> HasPendingOfferAsync(
        int listingId,
        string buyerId)
    {
        return await _context.Offers.AnyAsync(o =>
            o.ListingId == listingId &&
            o.BuyerId == buyerId &&
            o.Status == OfferStatus.Pending);
    }

    public async Task<Offer> AddAsync(Offer offer)
    {
        _context.Offers.Add(offer);

        await _context.SaveChangesAsync();

        return offer;
    }

    public async Task UpdateAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<bool> AcceptOfferAsync(Offer offer)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var soldDate = DateTime.UtcNow;

            // Only one request may change the listing from Active to Sold.
            int updatedListings = await _context.Listings
                .Where(l =>
                    l.Id == offer.ListingId &&
                    l.Status == ListingStatus.Active)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        l => l.Status,
                        ListingStatus.Sold)
                    .SetProperty(
                        l => l.SoldPrice,
                        offer.Amount)
                    .SetProperty(
                        l => l.SoldDate,
                        soldDate));

            if (updatedListings != 1)
            {
                await transaction.RollbackAsync();
                return false;
            }

            // The selected offer must still be Pending.
            int updatedOffers = await _context.Offers
                .Where(o =>
                    o.Id == offer.Id &&
                    o.ListingId == offer.ListingId &&
                    o.Status == OfferStatus.Pending)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        o => o.Status,
                        OfferStatus.Accepted));

            if (updatedOffers != 1)
            {
                await transaction.RollbackAsync();
                return false;
            }

            // Reject all other pending offers for the same listing.
            await _context.Offers
                .Where(o =>
                    o.ListingId == offer.ListingId &&
                    o.Id != offer.Id &&
                    o.Status == OfferStatus.Pending)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        o => o.Status,
                        OfferStatus.Rejected));

            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
