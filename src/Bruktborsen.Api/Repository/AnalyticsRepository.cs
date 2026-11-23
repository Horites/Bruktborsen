using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Api.Repository;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly ApplicationDbContext _context;

    public AnalyticsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Category?> GetCategoryByIdAsync(
        int categoryId)
    {
        return await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId);
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        return await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<Listing>> GetSoldListingsByCategoryAsync(
        int categoryId)
    {
        return await _context.Listings
            .AsNoTracking()
            .Include(l => l.PriceChanges)
            .Where(l =>
                l.CategoryId == categoryId &&
                l.Status == ListingStatus.Sold &&
                l.SoldPrice != null)
            .OrderBy(l => l.Id)
            .ToListAsync();
    }

    public async Task<List<Listing>> GetAllListingsAsync()
    {
        return await _context.Listings
            .AsNoTracking()
            .Include(l => l.Category)
            .Include(l => l.PriceChanges)
            .OrderBy(l => l.Id)
            .ToListAsync();
    }

    public async Task<Listing?> GetListingByIdAsync(
        int listingId)
    {
        return await _context.Listings
            .AsNoTracking()
            .Include(l => l.Category)
            .FirstOrDefaultAsync(l => l.Id == listingId);
    }

    public async Task<List<PriceChange>> GetPriceHistoryAsync(
        int listingId)
    {
        return await _context.PriceChanges
            .AsNoTracking()
            .Where(p => p.ListingId == listingId)
            .OrderBy(p => p.ChangedDate)
            .ThenBy(p => p.Id)
            .ToListAsync();
    }
}
