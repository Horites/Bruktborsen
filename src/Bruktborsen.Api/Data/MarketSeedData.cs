using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Api.Data;

public static class MarketSeedData
{
    private const string SeedPrefix = "Demo 2026 - ";

    public static async Task InitializeAsync(
        ApplicationDbContext context)
    {
        // Idempotency: Never create the demo market twice.
        if (await context.Listings.AnyAsync(
            l => l.Title.StartsWith(SeedPrefix)))
        {
            return;
        }

        var sellers = await context.Users
            .Where(u =>
                u.Email == "ola@brukt.no" ||
                u.Email == "kari@brukt.no" ||
                u.Email == "per@brukt.no")
            .OrderBy(u => u.Email)
            .ToListAsync();

        if (sellers.Count != 3)
        {
            throw new InvalidOperationException(
                "Seed users Ola, Kari and Per must exist first.");
        }

        var categories = new[]
        {
            "Elektronikk",
            "Møbler",
            "Sport",
            "Bøker",
            "Klær"
        };

        foreach (var name in categories)
        {
            if (!await context.Categories.AnyAsync(c => c.Name == name))
            {
                context.Categories.Add(new Category
                {
                    Name = name
                });
            }
        }

        await context.SaveChangesAsync();

        var categoryLookup = await context.Categories
            .Where(c => categories.Contains(c.Name))
            .ToDictionaryAsync(c => c.Name, c => c.Id);

        // The last two categories intentionally have few or no sales.
        var groups = new[]
        {
            new
            {
                Category = "Elektronikk",
                SoldCount = 7,
                BasePrice = 10000,
                Titles = new[]
                {
                    "Bærbar PC", "Gaming-skjerm", "Mobiltelefon",
                    "Nettbrett", "Hodetelefoner", "Spillkonsoll",
                    "Kamera", "Tastatur", "Grafikkort", "Høyttaler"
                }
            },
            new
            {
                Category = "Møbler",
                SoldCount = 7,
                BasePrice = 3500,
                Titles = new[]
                {
                    "Sofa", "Spisebord", "Kontorstol",
                    "Kommode", "Bokhylle", "TV-benk",
                    "Lenestol", "Skrivebord", "Nattbord", "Seng"
                }
            },
            new
            {
                Category = "Sport",
                SoldCount = 7,
                BasePrice = 2500,
                Titles = new[]
                {
                    "Terrengsykkel", "Langrennsski", "Snowboard",
                    "Treningsklokke", "Telt", "Kajakk",
                    "Hjelm", "Tursko", "Fiskestang", "Ryggsekk"
                }
            },
            new
            {
                Category = "Bøker",
                SoldCount = 1,
                BasePrice = 300,
                Titles = new[]
                {
                    "Programmeringsbok", "Krimroman", "Kokebok",
                    "Historiebok", "Fantasybok", "Lærebok",
                    "Reiseguide", "Biografi", "Tegneserie", "Ordbok"
                }
            },
            new
            {
                Category = "Klær",
                SoldCount = 0,
                BasePrice = 700,
                Titles = new[]
                {
                    "Vinterjakke", "Ullgenser", "Turjakke",
                    "Jeans", "Skjorte", "Regnjakke",
                    "Joggesko", "Dressjakke", "Lue", "Hansker"
                }
            }
        };

        var conditions = new[]
        {
            Condition.New,
            Condition.LikeNew,
            Condition.Good,
            Condition.Used,
            Condition.Defect
        };

        var referenceDate = DateTime.UtcNow.Date.AddDays(-60);

        int globalIndex = 0;

        foreach (var group in groups)
        {
            int categoryId = categoryLookup[group.Category];

            for (int i = 0; i < group.Titles.Length; i++)
            {
                var condition = conditions[i % conditions.Length];

                // Varied, positive listing prices.
                int price = group.BasePrice + i * 125;

                var status = i < group.SoldCount
                    ? ListingStatus.Sold
                    : i == 9
                        ? ListingStatus.Removed
                        : ListingStatus.Active;

                // One deliberately extreme asking price.
                if (group.Category == "Elektronikk" && i == 8)
                {
                    price = 100000;
                }

                var createdDate = referenceDate.AddDays(
                    globalIndex);

                int? soldPrice = status == ListingStatus.Sold
                    ? (int)Math.Round(
                        price * (0.75m + (i % 3) * 0.05m),
                        MidpointRounding.AwayFromZero)
                    : null;

                var listing = new Listing
                {
                    Title = SeedPrefix + group.Titles[i],
                    Description =
                        "Demonstrasjonsannonse for Bruktbørsen.",
                    Price = price,
                    Condition = condition,
                    Status = status,
                    CategoryId = categoryId,
                    SellerId = sellers[globalIndex % sellers.Count].Id,
                    CreatedDate = createdDate,
                    SoldDate = soldPrice.HasValue
                        ? createdDate.AddDays(2 + i)
                        : null,
                    SoldPrice = soldPrice
                };

                context.Listings.Add(listing);

                globalIndex++;
            }
        }

        await context.SaveChangesAsync();
    }
}
