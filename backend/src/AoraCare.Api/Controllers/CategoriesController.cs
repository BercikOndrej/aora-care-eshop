using AoraCare.Api.Controllers.Extension;
using AoraCare.Application.Dtos;
using AoraCare.Application.Services.Interfaces;
using AoraCare.Application.Validators;
using Microsoft.AspNetCore.Mvc;

namespace AoraCare.Api.Controllers;

/// <summary>
///     Controller mapping for category operations
/// </summary>
[ApiController]
[Route("[controller]")]
public class CategoriesController : ControllerBase
{
    private ICategoryService _categoryService;
    private CategoryAddDtoValidator _addDtoValidator;
    private CategoryUpdateDtoValidator _updateDtoValidator;

    /// <summary>
    ///     Default constructor
    /// </summary>
    /// <param name="categoryService">
    ///     Service provides operation for Categories
    /// </param>
    public CategoriesController(
        ICategoryService categoryService,
        CategoryAddDtoValidator addDtoValidator,
        CategoryUpdateDtoValidator updateDtoValidator
    )
    {
        _categoryService = categoryService;
        _addDtoValidator = addDtoValidator;
        _updateDtoValidator = updateDtoValidator;
    }

    /// <summary>
    ///     Get all categories.
    /// </summary>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    /// <returns>
    ///     Returns all <see cref="CategoryResponseDto"/>.
    /// </returns>
    /// <response code="200">Returns all categories</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    // Admin endpoint
    public async Task<ActionResult<List<CategoryResponseDto>>> GetAllAsync(
        CancellationToken ct = default
    ) => await _categoryService.GetAllAsync(ct);

    /// <summary>
    ///     Get all active categories. Active = visible for a end user.
    /// </summary>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    /// <returns>
    ///     Returns all active <see cref="CategoryResponseDto"/>.
    /// </returns>
    /// <response code="200">Returns all categories.</response>
    [HttpGet("active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CategoryResponseDto>>> GetAllActiveAsync(
        CancellationToken ct = default
    ) => await _categoryService.GetAllActiveAsync(ct);

    /// <summary>
    ///     Get category for a given id.
    /// </summary>
    /// <param name="id">
    ///     The identifier of the category to retrieve.
    /// </param>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    /// <returns>
    ///     Returns category for a given <paramref name="id"/> as <see cref="CategoryDetailResponseDto"/>.
    /// </returns>
    /// <response code="200">Returns category for the given id</response>
    /// <response code="404">If category does not exist in the database.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDetailResponseDto>> GetByIdAsync(
        Guid id,
        CancellationToken ct = default
    ) =>
        (await _categoryService.GetByIdAsync(id, ct)).ToActionResult<CategoryDetailResponseDto>(
            this
        );

    /// <summary>
    ///     Create new category.
    /// </summary>
    /// <param name="category">
    ///     Category to create in shape <see cref="CategoryAddDto"/>
    /// </param>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    /// <returns>
    ///     Newly created <see cref="CategoryResponseDto" />
    /// </returns>
    /// <response code="201">Returns the newly created category.</response>
    /// <response code="400">If the request body fails validation.</response>
    /// <response code="409">If slug created from name is not unique.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponseDto>> AddAsync(
        CategoryAddDto category,
        CancellationToken ct = default
    )
    {
        var validation = _addDtoValidator.Validate(category);
        if (!validation.IsValid)
        {
            validation.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        var result = await _categoryService.AddAsync(category, ct);
        return result.ToActionResult<CategoryResponseDto>(this, StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Partially updates a category
    /// </summary>
    /// <param name="id">
    ///     Identifier of the category to update
    /// </param>
    /// <param name="data">
    ///     Data for updating category. All fields are optional. If no new data is provided the original data remains.
    /// </param>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    /// <returns>
    ///     Return updated <see cref="CategoryResponseDto"/> for the given id.
    /// </returns>
    /// <response code="200">Returns the updated category.</response>
    /// <response code="400">If the request body fails validation.</response>
    /// <response code="404">If category with the given id does not exist.</response>
    /// <response code="409">If slug created from the new name is not unique.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponseDto>> UpdateAsync(
        Guid id,
        CategoryUpdateDto data,
        CancellationToken ct = default
    )
    {
        var validation = _updateDtoValidator.Validate(data);
        if (!validation.IsValid)
        {
            validation.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        var result = await _categoryService.UpdateAsync(id, data, ct);
        return result.ToActionResult<CategoryResponseDto>(this);
    }

    /// <summary>
    ///     Delete category for a given id.
    /// </summary>
    /// <param name="id">
    ///     The identifier of the category
    /// </param>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    /// <returns>
    ///     Returns no content
    /// </returns>
    /// <response code="204">Returns action to confirm successful operation</response>
    /// <response code="404">If category does not exists.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct = default) =>
        (await _categoryService.DeleteAsync(id, ct)).ToActionResult(
            this,
            StatusCodes.Status204NoContent
        );
}
