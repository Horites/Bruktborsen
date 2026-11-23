using System.ComponentModel.DataAnnotations;

namespace Bruktborsen.Api.DTOs.Offers;

public sealed class CreateOfferDto
{
    [Range(1, int.MaxValue)]
    public int Amount { get; set; }
}
