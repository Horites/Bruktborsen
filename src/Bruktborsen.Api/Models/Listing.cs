using System.ComponentModel.DataAnnotations;
using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.Models;

public class Listing
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    public int Price { get; set; }

    public Condition Condition { get; set; }

    public ListingStatus Status { get; set; } = ListingStatus.Active;

    public string? ImageUrl { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? SoldDate { get; set; }

    public int? SoldPrice { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    [Required]
    public string SellerId { get; set; } = string.Empty;

    public ApplicationUser Seller { get; set; } = null!;

    public ICollection<Offer> Offers { get; set; } = new List<Offer>();

    public ICollection<PriceChange> PriceChanges { get; set; } =
        new List<PriceChange>();

    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}