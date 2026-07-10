using aoraCareApi.Application.Dtos;
using aoraCareApi.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace aoraCareApi.Controllers;

/// <summary>
///     Controller mapping for category iperations
/// </summary>
[ApiController]
[Route("[controller]")]
public class CategoriesController : ControllerBase
{
    private ICategoryService _categoryService;

    /// <summary>
    ///     Default constructor
    /// </summary>
    /// <param name="categoryService">
    ///     Service provides operation for Categories
    /// </param>
    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    ///     Get all categories.
    /// </summary>
    /// <returns>
    ///     Returns all <see cref="CategoryResponseDto"/>.
    /// </returns>
    /// <response code="200">Returns all categories</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    // Admin endpoint
    public async Task<ActionResult<List<CategoryResponseDto>>> GetAllAsync() =>
        await _categoryService.GetAllAsync();

    /// <summary>
    ///     Get all active categories. Active = visible for a end user.
    /// </summary>
    /// <returns>
    ///     Returns all active <see cref="CategoryResponseDto"/>.
    /// </returns>
    /// <response code="200">Returns all categories.</response>
    [HttpGet("active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CategoryResponseDto>>> GetAllActiveAsync() =>
        await _categoryService.GetAllActiveAsync();

    /// <summary>
    ///     Get category for a given id.
    /// </summary>
    /// <param name="id">
    ///     The identifier of the category to retrieve.
    /// </param>
    /// <returns>
    ///     Returns category for a given <paramref name="id"/> <see cref="CategoryResponseDto"/>>
    /// </returns>
    /// <response code="200">Returns category for the given id</response>
    /// <response code="404">If category does not exist in the database.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponseDto>> GetAsync(Guid id)
    {
        var category = await _categoryService.GetAsync(id);
        return category is not null ? category : NotFound($"Category with {id} not found");
    }

    /// <summary>
    ///     Create new category.
    /// </summary>
    /// <param name="category">
    ///     Category to create in shape <see cref="CategoryAddDto"/>
    /// </param>
    /// <returns>
    ///     Newly created <see cref="CategoryResponseDto" />
    /// </returns>
    /// <response code="201">Returns the newly created category.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<CategoryResponseDto>> AddAsync(CategoryAddDto category)
    {
        var created = await _categoryService.AddAsync(category);

        return CreatedAtAction(nameof(GetAsync), new { Id = created.Id }, created);
    }

    /// <summary>
    ///     Partially updates a category
    /// </summary>
    /// <param name="id">
    ///     Identifier of the category to update
    /// </param>
    /// <param name="data">
    ///     Data for updating category. All fields are optional. Id no new data is provided the original data remains.
    /// </param>
    /// <returns>
    ///     Return updated <see cref="CategoryResponseDto"/> for the given <paramref name="id"/>.
    /// </returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponseDto>> UpdateAsync(
        Guid id,
        CategoryUpdateDto data
    )
    {
        var updated = await _categoryService.UpdateAsync(id, data);
        return updated is not null ? updated : NotFound("Category not found");
    }

    /// <summary>
    ///     Delete category for a given id.
    /// </summary>
    /// <param name="id">
    ///     The identifier of the category
    /// </param>
    /// <returns>
    ///     Returns no content
    /// </returns>
    /// <response code="204">Returns action to confirm successful operation</response>
    /// <response code="404">If category does not exists.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        bool success = await _categoryService.DeleteAsync(id);
        return success ? NoContent() : NotFound("Category not found");
    }
}
