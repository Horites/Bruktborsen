
using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repository;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Tests.BusinessRules;

public class OfferAcceptanceTests
{
    [Fact]
    public async Task AcceptOffer_SellsListing_AndRejectsOtherPendingOffers()
    {
        // Arrange: Real SQLite database in memory.
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);

        await context.Database.EnsureCreatedAsync();

        var seller = new ApplicationUser
        {
            Id = "seller-1",
            UserName = "seller@test.no",
            FullName = "Seller"
        };

        var buyer1 = new ApplicationUser
        {
            Id = "buyer-1",
            UserName = "buyer1@test.no",
            FullName = "Buyer One"
        };

        var buyer2 = new ApplicationUser
        {
            Id = "buyer-2",
            UserName = "buyer2@test.no",
            FullName = "Buyer Two"
        };

        var buyer3 = new ApplicationUser
        {
            Id = "buyer-3",
            UserName = "buyer3@test.no",
            FullName = "Buyer Three"
        };

        context.Users.AddRange(seller, buyer1, buyer2, buyer3);

        var category = new Category
        {
            Name = "Elektronikk"
        };

        context.Categories.Add(category);

        var listing = new Listing
        {
            Title = "Gaming PC",
            Price = 10000,
            Condition = Condition.Good,
            Status = ListingStatus.Active,
            Seller = seller,
            Category = category
        };

        var acceptedOffer = new Offer
        {
            Listing = listing,
            Buyer = buyer1,
            Amount = 8000,
            Status = OfferStatus.Pending
        };

        var rejectedOffer = new Offer
        {
            Listing = listing,
            Buyer = buyer2,
            Amount = 9000,
            Status = OfferStatus.Pending
        };

        var withdrawnOffer = new Offer
        {
            Listing = listing,
            Buyer = buyer3,
            Amount = 7000,
            Status = OfferStatus.Withdrawn
        };

        context.Offers.AddRange(
            acceptedOffer,
            rejectedOffer,
            withdrawnOffer
        );

        await context.SaveChangesAsync();

        var offerId = acceptedOffer.Id;
        var listingId = listing.Id;

        context.ChangeTracker.Clear();

        var repository = new OfferRepository(context);

        var offer = await repository.GetByIdAsync(offerId);

        Assert.NotNull(offer);

        // Act
        await repository.AcceptOfferAsync(offer);

        context.ChangeTracker.Clear();

        // Assert: Verify persisted database state.
        var savedListing = await context.Listings
            .AsNoTracking()
            .SingleAsync(l => l.Id == listingId);

        var savedOffers = await context.Offers
            .AsNoTracking()
            .Where(o => o.ListingId == listingId)
            .ToListAsync();

        Assert.Equal(ListingStatus.Sold, savedListing.Status);
        Assert.Equal(8000, savedListing.SoldPrice);
        Assert.NotNull(savedListing.SoldDate);

        Assert.Equal(
            OfferStatus.Accepted,
            savedOffers.Single(o => o.BuyerId == "buyer-1").Status
        );

        Assert.Equal(
            OfferStatus.Rejected,
            savedOffers.Single(o => o.BuyerId == "buyer-2").Status
        );

        Assert.Equal(
            OfferStatus.Withdrawn,
            savedOffers.Single(o => o.BuyerId == "buyer-3").Status
        );
    }
}
