
using Bruktborsen.Api.DTOs.Categories;

namespace Bruktborsen.Api.Services.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();

    Task<CategoryDto?> GetByIdAsync(int id);

    Task<(CategoryResult Result, CategoryDto? Category)> CreateAsync(
        CreateCategoryDto dto);

    Task<CategoryResult> UpdateAsync(
        int id,
        UpdateCategoryDto dto);

    Task<CategoryResult> DeleteAsync(int id);
}
