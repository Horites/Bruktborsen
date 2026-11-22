using System.ComponentModel.DataAnnotations;
using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.Models;

public class Offer
{
    public int Id { get; set; }

    [Required]
    public int ListingId { get; set; }

    public Listing Listing { get; set; } = null!;

    [Required]
    public string BuyerId { get; set; } = string.Empty;

    public ApplicationUser Buyer { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int Amount { get; set; }

    public OfferStatus Status { get; set; } = OfferStatus.Pending;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}