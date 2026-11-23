using Bruktborsen.Api.Data;
using Bruktborsen.Api.DTOs.Listings;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repository;
using Bruktborsen.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Tests.BusinessRules;

public class ListingBusinessRuleTests
{
    [Fact]
    public async Task UpdatePrice_CreatesPriceChange_WithCorrectValues()
    {
        await using var db = await TestDatabase.CreateAsync();

        var dto = CreateUpdateDto(10000);

        var result = await db.Service.UpdateAsync(
            db.ListingId,
            dto,
            db.SellerId,
            isAdmin: false
        );

        Assert.Equal(ListingResult.Success, result);

        db.Context.ChangeTracker.Clear();

        var listing = await db.Context.Listings
            .AsNoTracking()
            .SingleAsync(l => l.Id == db.ListingId);

        var priceChange = await db.Context.PriceChanges
            .AsNoTracking()
            .SingleAsync(p => p.ListingId == db.ListingId);

        Assert.Equal(10000, listing.Price);
        Assert.Equal(12000, priceChange.OldPrice);
        Assert.Equal(10000, priceChange.NewPrice);
        Assert.True(priceChange.ChangedDate <= DateTime.UtcNow);
    }

    [Fact]
    public async Task UpdateSoldListing_IsRejected()
    {
        await using var db = await TestDatabase.CreateAsync();

        var listing = await db.Context.Listings.FindAsync(
            db.ListingId);

        Assert.NotNull(listing);

        listing.Status = ListingStatus.Sold;
        listing.SoldPrice = 11000;
        listing.SoldDate = DateTime.UtcNow;

        await db.Context.SaveChangesAsync();

        var dto = CreateUpdateDto(9000);

        var result = await db.Service.UpdateAsync(
            db.ListingId,
            dto,
            db.SellerId,
            isAdmin: false
        );

        Assert.Equal(ListingResult.Sold, result);

        db.Context.ChangeTracker.Clear();

        var savedListing = await db.Context.Listings
            .AsNoTracking()
            .SingleAsync(l => l.Id == db.ListingId);

        Assert.Equal(ListingStatus.Sold, savedListing.Status);
        Assert.Equal(12000, savedListing.Price);
        Assert.Equal(11000, savedListing.SoldPrice);

        Assert.Empty(await db.Context.PriceChanges.ToListAsync());
    }

    private static UpdateListingDto CreateUpdateDto(int price)
    {
        return new UpdateListingDto
        {
            Title = "Gaming PC",
            Description = "Updated description",
            Price = price,
            Condition = Condition.Good,
            CategoryId = 1
        };
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public ApplicationDbContext Context { get; }

        public ListingService Service { get; }

        public int ListingId { get; private set; }

        public string SellerId => "seller-1";

        private TestDatabase(
            SqliteConnection connection,
            ApplicationDbContext context)
        {
            _connection = connection;
            Context = context;

            Service = new ListingService(
                new ListingRepository(context)
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

                var category = new Category
                {
                    Id = 1,
                    Name = "Elektronikk"
                };

                var listing = new Listing
                {
                    Title = "Gaming PC",
                    Description = "Original description",
                    Price = 12000,
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
