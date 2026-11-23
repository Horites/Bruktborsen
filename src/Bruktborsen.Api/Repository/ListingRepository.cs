using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Api.Repository;

public class ListingRepository : IListingRepository
{
    private readonly ApplicationDbContext _context;

    public ListingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Listing>> GetActiveAsync(
        string? search,
        int? categoryId,
        Condition? condition,
        int? minPrice,
        int? maxPrice,
        string? sort)
    {
        var query = _context.Listings
            .AsNoTracking()
            .Include(l => l.Category)
            .Where(l => l.Status == ListingStatus.Active)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(l =>
                l.Title.Contains(term) ||
                (l.Description != null &&
                 l.Description.Contains(term)));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(l =>
                l.CategoryId == categoryId.Value);
        }

        if (condition.HasValue)
        {
            query = query.Where(l =>
                l.Condition == condition.Value);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(l =>
                l.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(l =>
                l.Price <= maxPrice.Value);
        }

        query = sort?.ToLowerInvariant() switch
        {
            "price_asc" => query
                .OrderBy(l => l.Price)
                .ThenBy(l => l.Id),

            "price_desc" => query
                .OrderByDescending(l => l.Price)
                .ThenBy(l => l.Id),

            _ => query
                .OrderByDescending(l => l.CreatedDate)
                .ThenByDescending(l => l.Id)
        };

        return await query.ToListAsync();
    }

    public async Task<Listing?> GetByIdAsync(int id)
    {
        return await _context.Listings
            .Include(l => l.Category)
            .Include(l => l.Seller)
            .Include(l => l.Offers)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<List<Listing>> GetBySellerAsync(
        string sellerId)
    {
        return await _context.Listings
            .AsNoTracking()
            .Include(l => l.Category)
            .Where(l => l.SellerId == sellerId)
            .OrderByDescending(l => l.CreatedDate)
            .ToListAsync();
    }

    public async Task<bool> CategoryExistsAsync(
        int categoryId)
    {
        return await _context.Categories
            .AnyAsync(c => c.Id == categoryId);
    }

    public async Task<Listing> AddAsync(Listing listing)
    {
        _context.Listings.Add(listing);

        await _context.SaveChangesAsync();

        return listing;
    }

    public async Task UpdateAsync(Listing listing)
    {
        await _context.SaveChangesAsync();
    }

    public async Task UpdatePriceAsync(
        Listing listing,
        PriceChange priceChange)
    {
        _context.PriceChanges.Add(priceChange);

        // Listing is tracked by the DbContext.
        // Both changes are saved in the same SaveChanges call.
        await _context.SaveChangesAsync();
    }
}
