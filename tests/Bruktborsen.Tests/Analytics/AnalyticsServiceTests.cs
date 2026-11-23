using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repository;
using Bruktborsen.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Tests.Analytics;

public class AnalyticsServiceTests
{
    [Fact]
    public async Task PriceSuggestion_NormalizesSalesForDifferentConditions()
    {
        await using var db = await TestDatabase.CreateAsync();

        await db.AddListingAsync(
            10000, Condition.New, ListingStatus.Sold, 10000);

        await db.AddListingAsync(
            7000, Condition.Good, ListingStatus.Sold, 7000);

        await db.AddListingAsync(
            5500, Condition.Used, ListingStatus.Sold, 5500);

        var result = await db.Service.GetPriceSuggestionAsync(
            db.CategoryId,
            Condition.LikeNew
        );

        Assert.NotNull(result);
        Assert.Equal(3, result.SoldCount);
        Assert.Equal(8500m, result.SuggestedPrice);
    }

    [Fact]
    public async Task PriceSuggestion_WithTwoSales_ReturnsNoSuggestion()
    {
        await using var db = await TestDatabase.CreateAsync();

        await db.AddListingAsync(
            10000, Condition.New, ListingStatus.Sold, 10000);

        await db.AddListingAsync(
            12000, Condition.New, ListingStatus.Sold, 12000);

        var result = await db.Service.GetPriceSuggestionAsync(
            db.CategoryId,
            Condition.Good
        );

        Assert.NotNull(result);
        Assert.Equal(2, result.SoldCount);
        Assert.Null(result.SuggestedPrice);
    }

    [Fact]
    public async Task PriceSuggestion_RoundsMedianToNearestTen()
    {
        await using var db = await TestDatabase.CreateAsync();

        foreach (int price in new[] { 10001, 10009, 10019 })
        {
            await db.AddListingAsync(
                price,
                Condition.New,
                ListingStatus.Sold,
                price
            );
        }

        var result = await db.Service.GetPriceSuggestionAsync(
            db.CategoryId,
            Condition.New
        );

        Assert.NotNull(result);
        Assert.Equal(10010m, result.SuggestedPrice);
    }

    [Theory]
    [InlineData(50, "Low")]
    [InlineData(120, "Normal")]
    [InlineData(200, "High")]
    public async Task PriceAssessment_UsesHistoricalSales(
        int askingPrice,
        string expected)
    {
        await using var db = await TestDatabase.CreateAsync();

        foreach (int price in new[] { 100, 110, 120, 130, 140 })
        {
            await db.AddListingAsync(
                price,
                Condition.New,
                ListingStatus.Sold,
                price
            );
        }

        int listingId = await db.AddListingAsync(
            askingPrice,
            Condition.New,
            ListingStatus.Active
        );

        var result = await db.Service.GetPriceAssessmentAsync(
            listingId
        );

        Assert.NotNull(result);
        Assert.Equal(5, result.ComparableSalesCount);
        Assert.Equal(expected, result.Assessment);
        Assert.Equal(80m, result.LowerFence);
        Assert.Equal(160m, result.UpperFence);
    }

    [Fact]
    public async Task CategoryStats_UsesOriginalPriceForDiscount()
    {
        await using var db = await TestDatabase.CreateAsync();

        int listingId = await db.AddListingAsync(
            10000,
            Condition.Good,
            ListingStatus.Sold,
            8000
        );

        db.Context.PriceChanges.Add(new PriceChange
        {
            ListingId = listingId,
            OldPrice = 12000,
            NewPrice = 10000,
            ChangedDate = new DateTime(
                2026, 11, 5, 12, 0, 0, DateTimeKind.Utc)
        });

        await db.Context.SaveChangesAsync();

        var result = await db.Service.GetCategoryStatsAsync(
            db.CategoryId
        );

        Assert.NotNull(result);
        Assert.Equal(1, result.SoldCount);
        Assert.Equal(8000m, result.AverageSoldPrice);
        Assert.Equal(8000m, result.MedianSoldPrice);
        Assert.Null(result.SampleStandardDeviation);
        Assert.Equal(1m, result.SoldFraction);

        // (12000 - 8000) / 12000 * 100 = 33.333... %
        Assert.NotNull(result.AverageDiscountPercent);
        Assert.InRange(
            result.AverageDiscountPercent.Value,
            33.33m,
            33.34m
        );

        Assert.NotNull(result.AverageDaysToSell);
        Assert.Equal(10d, result.AverageDaysToSell.Value, 6);
    }

    [Fact]
    public async Task Overview_CalculatesListingCountsAndSoldFraction()
    {
        await using var db = await TestDatabase.CreateAsync();

        await db.AddListingAsync(
            1000, Condition.Good, ListingStatus.Active);

        await db.AddListingAsync(
            2000, Condition.Good, ListingStatus.Sold, 1500);

        await db.AddListingAsync(
            3000, Condition.Good, ListingStatus.Removed);

        var result = await db.Service.GetOverviewAsync();

        Assert.Equal(3, result.TotalListings);
        Assert.Equal(1, result.ActiveListings);
        Assert.Equal(1, result.SoldListings);
        Assert.Equal(1, result.RemovedListings);
        Assert.Equal(1500m, result.AverageSoldPrice);
        Assert.Equal(1m / 3m, result.SoldFraction);

        var category = Assert.Single(result.Categories);

        Assert.Equal(1, category.ActiveCount);
        Assert.Equal(1, category.SoldCount);
        Assert.Equal(1, category.RemovedCount);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public ApplicationDbContext Context { get; }

        public AnalyticsService Service { get; }

        public int CategoryId { get; private set; }

        private const string SellerId = "analytics-seller";

        private TestDatabase(
            SqliteConnection connection,
            ApplicationDbContext context)
        {
            _connection = connection;
            Context = context;

            Service = new AnalyticsService(
                new AnalyticsRepository(context)
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

                context.Users.Add(new ApplicationUser
                {
                    Id = SellerId,
                    UserName = "analytics@test.no",
                    FullName = "Analytics Test Seller"
                });

                var category = new Category
                {
                    Name = "Test Category"
                };

                context.Categories.Add(category);

                await context.SaveChangesAsync();

                var database = new TestDatabase(
                    connection,
                    context
                );

                database.CategoryId = category.Id;

                return database;
            }
            catch
            {
                await context.DisposeAsync();
                await connection.DisposeAsync();
                throw;
            }
        }

        public async Task<int> AddListingAsync(
            int price,
            Condition condition,
            ListingStatus status,
            int? soldPrice = null)
        {
            var created = new DateTime(
                2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

            var listing = new Listing
            {
                Title = $"Test listing {price}",
                Description = "Analytics test data",
                Price = price,
                Condition = condition,
                Status = status,
                CategoryId = CategoryId,
                SellerId = SellerId,
                CreatedDate = created,
                SoldPrice = soldPrice,
                SoldDate = status == ListingStatus.Sold
                    ? created.AddDays(10)
                    : null
            };

            Context.Listings.Add(listing);
            await Context.SaveChangesAsync();

            return listing.Id;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
