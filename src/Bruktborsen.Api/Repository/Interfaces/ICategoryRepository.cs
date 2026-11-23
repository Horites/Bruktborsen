using Bruktborsen.Api.Models;

namespace Bruktborsen.Api.Repository.Interfaces;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync();

    Task<Category?> GetByIdAsync(int id);

    Task<Category?> GetByNameAsync(string name);

    Task<bool> HasListingsAsync(int categoryId);

    Task<Category> AddAsync(Category category);

    Task UpdateAsync(Category category);

    Task DeleteAsync(Category category);
}
