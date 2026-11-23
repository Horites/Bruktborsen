using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Api.Data;

public static class MarketInteractionSeedData
{
    private const string SeedPrefix = "Demo 2026 - ";

    public static async Task InitializeAsync(
        ApplicationDbContext context)
    {
        var users = await context.Users
            .AsNoTracking()
            .Where(u =>
                u.Email == "ola@brukt.no" ||
                u.Email == "kari@brukt.no" ||
                u.Email == "per@brukt.no")
            .OrderBy(u => u.Email)
            .ToListAsync();

        if (users.Count != 3)
        {
            throw new InvalidOperationException(
                "The three demo users must exist before seeding.");
        }

        var listings = await context.Listings
            .AsNoTracking()
            .Where(l => l.Title.StartsWith(SeedPrefix))
            .OrderBy(l => l.Id)
            .ToListAsync();

        if (listings.Count == 0)
        {
            throw new InvalidOperationException(
                "MarketSeedData must run before interaction seeding.");
        }

        // Use one transaction for all interaction seed records.
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        try
        {
            await SeedOffersAsync(context, listings, users);

            await SeedFavoritesAsync(context, listings, users);

            await SeedPriceChangesAsync(context, listings);

            await context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task SeedOffersAsync(
        ApplicationDbContext context,
        List<Listing> listings,
        List<ApplicationUser> users)
    {
        var soldListings = listings
            .Where(l => l.Status == ListingStatus.Sold)
            .Take(5);

        foreach (var listing in soldListings)
        {
            // Do not create duplicate seed offers on restart.
            if (await context.Offers.AnyAsync(
                o => o.ListingId == listing.Id))
            {
                continue;
            }

            var buyers = users
                .Where(u => u.Id != listing.SellerId)
                .ToArray();

            var acceptedBuyer = buyers[0];
            var otherBuyer = buyers[1];

            var offerDate = listing.CreatedDate.AddHours(12);

            context.Offers.AddRange(
                new Offer
                {
                    ListingId = listing.Id,
                    BuyerId = acceptedBuyer.Id,
                    Amount = listing.SoldPrice!.Value,
                    Status = OfferStatus.Accepted,
                    CreatedDate = offerDate
                },
                new Offer
                {
                    ListingId = listing.Id,
                    BuyerId = otherBuyer.Id,
                    Amount = Math.Max(1, listing.Price * 9 / 10),
                    Status = OfferStatus.Rejected,
                    CreatedDate = offerDate.AddHours(1)
                },
                new Offer
                {
                    ListingId = listing.Id,
                    BuyerId = otherBuyer.Id,
                    Amount = Math.Max(1, listing.Price * 8 / 10),
                    Status = OfferStatus.Withdrawn,
                    CreatedDate = offerDate.AddHours(2)
                }
            );
        }

        var activeListings = listings
            .Where(l => l.Status == ListingStatus.Active)
            .Take(5);

        foreach (var listing in activeListings)
        {
            if (await context.Offers.AnyAsync(
                o => o.ListingId == listing.Id))
            {
                continue;
            }

            var buyers = users
                .Where(u => u.Id != listing.SellerId)
                .ToArray();

            context.Offers.AddRange(
                new Offer
                {
                    ListingId = listing.Id,
                    BuyerId = buyers[0].Id,
                    Amount = Math.Max(1, listing.Price * 8 / 10),
                    Status = OfferStatus.Pending,
                    CreatedDate = listing.CreatedDate.AddDays(1)
                },
                new Offer
                {
                    ListingId = listing.Id,
                    BuyerId = buyers[1].Id,
                    Amount = Math.Max(1, listing.Price * 7 / 10),
                    Status = OfferStatus.Withdrawn,
                    CreatedDate = listing.CreatedDate.AddHours(12)
                }
            );
        }
    }

    private static async Task SeedFavoritesAsync(
        ApplicationDbContext context,
        List<Listing> listings,
        List<ApplicationUser> users)
    {
        var favorites = listings
            .Where(l => l.Status == ListingStatus.Active)
            .Take(10)
            .ToArray();

        for (int i = 0; i < favorites.Length; i++)
        {
            var listing = favorites[i];

            var user = users
                .Where(u => u.Id != listing.SellerId)
                .ElementAt(i % 2);

            bool exists = await context.Favorites.AnyAsync(
                f => f.ListingId == listing.Id &&
                     f.UserId == user.Id);

            if (!exists)
            {
                context.Favorites.Add(new Favorite
                {
                    ListingId = listing.Id,
                    UserId = user.Id
                });
            }
        }
    }

    private static async Task SeedPriceChangesAsync(
        ApplicationDbContext context,
        List<Listing> listings)
    {
        var selectedListings = listings
            .Where(l =>
                l.Status == ListingStatus.Sold ||
                l.Status == ListingStatus.Active)
            .Take(10);

        foreach (var listing in selectedListings)
        {
            bool hasHistory = await context.PriceChanges.AnyAsync(
                p => p.ListingId == listing.Id);

            if (hasHistory)
            {
                continue;
            }

            context.PriceChanges.Add(new PriceChange
            {
                ListingId = listing.Id,
                OldPrice = listing.Price + 500,
                NewPrice = listing.Price,
                ChangedDate = listing.CreatedDate.AddHours(6)
            });
        }
    }
}
