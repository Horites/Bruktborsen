using System.ComponentModel.DataAnnotations;

namespace Bruktborsen.Api.Models;

public class PriceChange
{
    public int Id { get; set; }

    [Required]
    public int ListingId { get; set; }

    public Listing Listing { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int OldPrice { get; set; }

    [Range(1, int.MaxValue)]
    public int NewPrice { get; set; }

    public DateTime ChangedDate { get; set; } = DateTime.UtcNow;
}