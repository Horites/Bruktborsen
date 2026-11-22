using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<PriceChange> PriceChanges => Set<PriceChange>();
    public DbSet<Favorite> Favorites => Set<Favorite>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Categories
        builder.Entity<Category>(entity =>
        {
            entity.HasIndex(c => c.Name).IsUnique();

            entity
                .HasMany(c => c.Listings)
                .WithOne(l => l.Category)
                .HasForeignKey(l => l.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Listings
        builder.Entity<Listing>(entity =>
        {
            entity.Property(l => l.Condition).HasConversion<string>();

            entity.Property(l => l.Status).HasConversion<string>();

            entity.HasIndex(l => l.CategoryId);
            entity.HasIndex(l => l.SellerId);
            entity.HasIndex(l => l.Status);

            entity
                .HasOne(l => l.Seller)
                .WithMany(u => u.Listings)
                .HasForeignKey(l => l.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasMany(l => l.Offers)
                .WithOne(o => o.Listing)
                .HasForeignKey(o => o.ListingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasMany(l => l.PriceChanges)
                .WithOne(p => p.Listing)
                .HasForeignKey(p => p.ListingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasMany(l => l.Favorites)
                .WithOne(f => f.Listing)
                .HasForeignKey(f => f.ListingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Offers
        builder.Entity<Offer>(entity =>
        {
            entity.Property(o => o.Status).HasConversion<string>();

            entity.HasIndex(o => o.ListingId);
            entity.HasIndex(o => o.BuyerId);

            entity
                .HasOne(o => o.Buyer)
                .WithMany(u => u.Offers)
                .HasForeignKey(o => o.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            // One pending offer per buyer per listing.
            entity
                .HasIndex(o => new { o.ListingId, o.BuyerId })
                .IsUnique()
                .HasFilter("\"Status\" = 'Pending'");
        });

        // PriceChanges
        builder.Entity<PriceChange>(entity =>
        {
            entity.HasIndex(p => new { p.ListingId, p.ChangedDate });
        });

        // Favorites
        builder.Entity<Favorite>(entity =>
        {
            entity.HasIndex(f => new { f.ListingId, f.UserId }).IsUnique();

            entity
                .HasOne(f => f.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
