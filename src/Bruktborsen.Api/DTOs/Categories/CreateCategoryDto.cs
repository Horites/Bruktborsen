using System.ComponentModel.DataAnnotations;

namespace Bruktborsen.Api.DTOs.Categories;

public sealed class CreateCategoryDto
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}