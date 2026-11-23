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

    public async Task AcceptOfferAsync(Offer offer)
    {
        // All changes must be persisted atomically.
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var listing = offer.Listing;

            offer.Status = OfferStatus.Accepted;

            listing.Status = ListingStatus.Sold;
            listing.SoldPrice = offer.Amount;
            listing.SoldDate = DateTime.UtcNow;

            foreach (var otherOffer in listing.Offers)
            {
                if (otherOffer.Id != offer.Id &&
                    otherOffer.Status == OfferStatus.Pending)
                {
                    otherOffer.Status = OfferStatus.Rejected;
                }
            }

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
