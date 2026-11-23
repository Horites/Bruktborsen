using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repository;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Tests.BusinessRules;

public class OfferConcurrencyTests
{
    [Fact]
    public async Task AcceptOffer_WithStaleData_OnlyFirstOfferSucceeds()
    {
        // Arrange: Two independent DbContexts use the same SQLite database.
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"bruktborsen-concurrency-{Guid.NewGuid():N}.db"
        );

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        try
        {
            int firstOfferId;
            int secondOfferId;
            int listingId;

            await using (var setupContext =
                new ApplicationDbContext(options))
            {
                await setupContext.Database.EnsureCreatedAsync();

                var seller = new ApplicationUser
                {
                    Id = "seller-1",
                    UserName = "seller@test.no",
                    FullName = "Test Seller"
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

                var category = new Category
                {
                    Name = "Elektronikk"
                };

                var listing = new Listing
                {
                    Title = "Gaming PC",
                    Description = "Concurrency test",
                    Price = 10000,
                    Condition = Condition.Good,
                    Status = ListingStatus.Active,
                    Category = category,
                    Seller = seller,
                    CreatedDate = DateTime.UtcNow
                };

                var firstOffer = new Offer
                {
                    Listing = listing,
                    Buyer = buyer1,
                    Amount = 8000,
                    Status = OfferStatus.Pending,
                    CreatedDate = DateTime.UtcNow
                };

                var secondOffer = new Offer
                {
                    Listing = listing,
                    Buyer = buyer2,
                    Amount = 9000,
                    Status = OfferStatus.Pending,
                    CreatedDate = DateTime.UtcNow
                };

                setupContext.Offers.AddRange(
                    firstOffer,
                    secondOffer
                );

                await setupContext.SaveChangesAsync();

                listingId = listing.Id;
                firstOfferId = firstOffer.Id;
                secondOfferId = secondOffer.Id;
            }

            await using var contextA =
                new ApplicationDbContext(options);

            await using var contextB =
                new ApplicationDbContext(options);

            var repositoryA = new OfferRepository(contextA);
            var repositoryB = new OfferRepository(contextB);

            // Both requests read the listing before any sale happens.
            var offerA = await repositoryA.GetByIdAsync(
                firstOfferId);

            var offerB = await repositoryB.GetByIdAsync(
                secondOfferId);

            Assert.NotNull(offerA);
            Assert.NotNull(offerB);

            Assert.Equal(
                ListingStatus.Active,
                offerA.Listing.Status
            );

            Assert.Equal(
                ListingStatus.Active,
                offerB.Listing.Status
            );

            // Act: Request A completes first.
            bool acceptedA = await repositoryA.AcceptOfferAsync(
                offerA);

            // Request B still holds an old copy of the listing.
            bool acceptedB = await repositoryB.AcceptOfferAsync(
                offerB);

            // Assert: Only the first sale may succeed.
            Assert.True(acceptedA);
            Assert.False(acceptedB);

            await using var verificationContext =
                new ApplicationDbContext(options);

            var savedListing = await verificationContext.Listings
                .AsNoTracking()
                .SingleAsync(l => l.Id == listingId);

            var savedOffers = await verificationContext.Offers
                .AsNoTracking()
                .Where(o => o.ListingId == listingId)
                .ToListAsync();

            Assert.Equal(
                ListingStatus.Sold,
                savedListing.Status
            );

            Assert.Equal(8000, savedListing.SoldPrice);
            Assert.NotNull(savedListing.SoldDate);

            Assert.Equal(
                OfferStatus.Accepted,
                savedOffers.Single(
                    o => o.Id == firstOfferId).Status
            );

            Assert.Equal(
                OfferStatus.Rejected,
                savedOffers.Single(
                    o => o.Id == secondOfferId).Status
            );

            Assert.Single(
                savedOffers,
                o => o.Status == OfferStatus.Accepted
            );
        }
        finally
        {
            // SQLite connections are disposed before cleanup.
            SqliteConnection.ClearAllPools();

            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                var path = databasePath + suffix;

                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}
