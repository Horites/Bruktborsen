using Bruktborsen.Api.Data;
using Bruktborsen.Api.DTOs.Offers;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repository;
using Bruktborsen.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Tests.BusinessRules;

public class OfferBusinessRuleTests
{
    [Fact]
    public async Task CreateOffer_OnOwnListing_IsRejected()
    {
        await using var db = await TestDatabase.CreateAsync();

        var result = await db.Service.CreateAsync(
            db.ListingId,
            new CreateOfferDto { Amount = 7000 },
            db.SellerId
        );

        Assert.Equal(OfferResult.OwnListing, result.Result);
        Assert.Empty(await db.Context.Offers.ToListAsync());
    }

    [Fact]
    public async Task CreateOffer_OnSoldListing_IsRejected()
    {
        await using var db = await TestDatabase.CreateAsync();

        var listing = await db.Context.Listings.FindAsync(
            db.ListingId);

        Assert.NotNull(listing);

        listing.Status = ListingStatus.Sold;
        listing.SoldPrice = 8000;
        listing.SoldDate = DateTime.UtcNow;

        await db.Context.SaveChangesAsync();

        var result = await db.Service.CreateAsync(
            db.ListingId,
            new CreateOfferDto { Amount = 8000 },
            db.BuyerId
        );

        Assert.Equal(
            OfferResult.ListingNotActive,
            result.Result
        );

        Assert.Empty(await db.Context.Offers.ToListAsync());
    }

    [Fact]
    public async Task CreateOffer_SecondPendingOffer_IsRejected()
    {
        await using var db = await TestDatabase.CreateAsync();

        var first = await db.Service.CreateAsync(
            db.ListingId,
            new CreateOfferDto { Amount = 7000 },
            db.BuyerId
        );

        var second = await db.Service.CreateAsync(
            db.ListingId,
            new CreateOfferDto { Amount = 8000 },
            db.BuyerId
        );

        Assert.Equal(OfferResult.Success, first.Result);
        Assert.Equal(
            OfferResult.DuplicatePending,
            second.Result
        );

        Assert.Equal(
            1,
            await db.Context.Offers.CountAsync()
        );
    }

    [Theory]
    [InlineData(4999, OfferResult.AmountTooLow)]
    [InlineData(5000, OfferResult.Success)]
    public async Task CreateOffer_EnforcesMinimumFiftyPercent(
        int amount,
        OfferResult expected)
    {
        await using var db = await TestDatabase.CreateAsync();

        var result = await db.Service.CreateAsync(
            db.ListingId,
            new CreateOfferDto { Amount = amount },
            db.BuyerId
        );

        Assert.Equal(expected, result.Result);

        if (expected == OfferResult.Success)
        {
            Assert.NotNull(result.Data);
            Assert.Equal(amount, result.Data.Amount);
            Assert.Equal(
                OfferStatus.Pending,
                result.Data.Status
            );
        }
        else
        {
            Assert.Null(result.Data);
            Assert.Empty(await db.Context.Offers.ToListAsync());
        }
    }

    [Fact]
    public async Task AcceptOffer_ByNonSeller_IsForbidden()
    {
        await using var db = await TestDatabase.CreateAsync();

        var created = await db.Service.CreateAsync(
            db.ListingId,
            new CreateOfferDto { Amount = 8000 },
            db.BuyerId
        );

        Assert.Equal(OfferResult.Success, created.Result);
        Assert.NotNull(created.Data);

        var result = await db.Service.AcceptAsync(
            created.Data.Id,
            db.OtherBuyerId
        );

        Assert.Equal(OfferResult.Forbidden, result);

        db.Context.ChangeTracker.Clear();

        var listing = await db.Context.Listings
            .SingleAsync(l => l.Id == db.ListingId);

        var offer = await db.Context.Offers
            .SingleAsync(o => o.Id == created.Data.Id);

        Assert.Equal(ListingStatus.Active, listing.Status);
        Assert.Equal(OfferStatus.Pending, offer.Status);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public ApplicationDbContext Context { get; }

        public OfferService Service { get; }

        public int ListingId { get; private set; }

        public string SellerId => "seller-1";

        public string BuyerId => "buyer-1";

        public string OtherBuyerId => "buyer-2";

        private TestDatabase(
            SqliteConnection connection,
            ApplicationDbContext context)
        {
            _connection = connection;
            Context = context;

            var listingRepository = new ListingRepository(context);
            var offerRepository = new OfferRepository(context);

            Service = new OfferService(
                offerRepository,
                listingRepository
            );
        }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection(
                "Data Source=:memory:"
            );

            await connection.OpenAsync();

            var options =
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlite(connection)
                    .Options;

            var context = new ApplicationDbContext(options);

            try
            {
                await context.Database.EnsureCreatedAsync();

                var seller = new ApplicationUser
                {
                    Id = "seller-1",
                    UserName = "seller@test.no",
                    FullName = "Test Seller"
                };

                var buyer = new ApplicationUser
                {
                    Id = "buyer-1",
                    UserName = "buyer@test.no",
                    FullName = "Test Buyer"
                };

                var otherBuyer = new ApplicationUser
                {
                    Id = "buyer-2",
                    UserName = "other@test.no",
                    FullName = "Other Buyer"
                };

                context.Users.AddRange(
                    seller,
                    buyer,
                    otherBuyer
                );

                var category = new Category
                {
                    Name = "Elektronikk"
                };

                var listing = new Listing
                {
                    Title = "Gaming PC",
                    Description = "Test listing",
                    Price = 10000,
                    Condition = Condition.Good,
                    Status = ListingStatus.Active,
                    Category = category,
                    Seller = seller,
                    CreatedDate = DateTime.UtcNow
                };

                context.Listings.Add(listing);

                await context.SaveChangesAsync();

                var database = new TestDatabase(
                    connection,
                    context
                );

                database.ListingId = listing.Id;

                return database;
            }
            catch
            {
                await context.DisposeAsync();
                await connection.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
