
using Bruktborsen.Api.DTOs.Categories;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Repository.Interfaces;
using Bruktborsen.Api.Services.Interfaces;

namespace Bruktborsen.Api.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;

    public CategoryService(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        var categories = await _repository.GetAllAsync();

        return categories
            .Select(c => new CategoryDto(c.Id, c.Name))
            .ToList();
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var category = await _repository.GetByIdAsync(id);

        return category is null
            ? null
            : new CategoryDto(category.Id, category.Name);
    }

    public async Task<(CategoryResult Result, CategoryDto? Category)>
        CreateAsync(CreateCategoryDto dto)
    {
        var name = dto.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
        {
            return (CategoryResult.InvalidName, null);
        }

        var existing = await _repository.GetByNameAsync(name);

        if (existing is not null)
        {
            return (CategoryResult.DuplicateName, null);
        }

        var category = new Category
        {
            Name = name
        };

        var created = await _repository.AddAsync(category);

        return (
            CategoryResult.Success,
            new CategoryDto(created.Id, created.Name)
        );
    }

    public async Task<CategoryResult> UpdateAsync(
        int id,
        UpdateCategoryDto dto)
    {
        var category = await _repository.GetByIdAsync(id);

        if (category is null)
        {
            return CategoryResult.NotFound;
        }

        var name = dto.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
        {
            return CategoryResult.InvalidName;
        }

        var existing = await _repository.GetByNameAsync(name);

        if (existing is not null && existing.Id != id)
        {
            return CategoryResult.DuplicateName;
        }

        category.Name = name;

        await _repository.UpdateAsync(category);

        return CategoryResult.Success;
    }

    public async Task<CategoryResult> DeleteAsync(int id)
    {
        var category = await _repository.GetByIdAsync(id);

        if (category is null)
        {
            return CategoryResult.NotFound;
        }

        if (await _repository.HasListingsAsync(id))
        {
            return CategoryResult.HasListings;
        }

        await _repository.DeleteAsync(category);

        return CategoryResult.Success;
    }
}
