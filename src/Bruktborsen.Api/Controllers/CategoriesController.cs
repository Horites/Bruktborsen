
using Bruktborsen.Api.DTOs.Categories;
using Bruktborsen.Api.Services;
using Bruktborsen.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bruktborsen.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    // GET /api/categories
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(List<CategoryDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CategoryDto>>> GetAll()
    {
        var categories = await _categoryService.GetAllAsync();

        return Ok(categories);
    }

    // GET /api/categories/{id}
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(CategoryDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetById(int id)
    {
        var category = await _categoryService.GetByIdAsync(id);

        if (category is null)
        {
            return NotFound();
        }

        return Ok(category);
    }

    // POST /api/categories
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(
        typeof(CategoryDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Create(
        [FromBody] CreateCategoryDto dto)
    {
        var (result, category) =
            await _categoryService.CreateAsync(dto);

        return result switch
        {
            CategoryResult.InvalidName =>
                BadRequest(new
                {
                    message = "Invalid category name."
                }),

            CategoryResult.DuplicateName =>
                Conflict(new
                {
                    message = "Category name already exists."
                }),

            CategoryResult.Success when category is not null =>
                CreatedAtAction(
                    nameof(GetById),
                    new { id = category.Id },
                    category),

            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    // PUT /api/categories/{id}
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateCategoryDto dto)
    {
        var result = await _categoryService.UpdateAsync(id, dto);

        return result switch
        {
            CategoryResult.Success => NoContent(),

            CategoryResult.NotFound => NotFound(),

            CategoryResult.InvalidName =>
                BadRequest(new
                {
                    message = "Invalid category name."
                }),

            CategoryResult.DuplicateName =>
                Conflict(new
                {
                    message = "Category name already exists."
                }),

            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    // DELETE /api/categories/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _categoryService.DeleteAsync(id);

        return result switch
        {
            CategoryResult.Success => NoContent(),

            CategoryResult.NotFound => NotFound(),

            CategoryResult.HasListings =>
                Conflict(new
                {
                    message = "Cannot delete a category with listings."
                }),

            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
