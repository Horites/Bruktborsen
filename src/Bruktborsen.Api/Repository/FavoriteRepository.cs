using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Api.Repository;

public class FavoriteRepository : IFavoriteRepository
{
    private readonly ApplicationDbContext _context;

    public FavoriteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Favorite>> GetByUserAsync(
        string userId)
    {
        return await _context.Favorites
            .AsNoTracking()
            .Include(f => f.Listing)
                .ThenInclude(l => l.Category)
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.Id)
            .ToListAsync();
    }

    public async Task<Favorite?> GetAsync(
        int listingId,
        string userId)
    {
        return await _context.Favorites
            .FirstOrDefaultAsync(f =>
                f.ListingId == listingId &&
                f.UserId == userId);
    }

    public async Task<bool> ListingExistsAsync(
        int listingId)
    {
        return await _context.Listings
            .AnyAsync(l => l.Id == listingId);
    }

    public async Task AddAsync(Favorite favorite)
    {
        _context.Favorites.Add(favorite);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Favorite favorite)
    {
        _context.Favorites.Remove(favorite);
        await _context.SaveChangesAsync();
    }
}
