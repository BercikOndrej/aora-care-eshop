using AoraCare.Api.Controllers.Extension;
using AoraCare.Application.Dtos;
using AoraCare.Application.Services.Interfaces;
using AoraCare.Application.Validators;
using Microsoft.AspNetCore.Mvc;

namespace AoraCare.Api.Controllers;

/// <summary>
///     Controller mapping for product operations
/// </summary>
[ApiController]
[Route("[controller]")]
public class ProductsController : ControllerBase
{
    private IProductService _productService;
    private ProductAddDtoValidator _addDtoValidator;
    private ProductUpdateDtoValidator _updateDtoValidator;

    /// <summary>
    ///     Default constructor
    /// </summary>
    /// <param name="productService">
    ///     Service provides operation for Products
    /// </param>
    public ProductsController(
        IProductService productService,
        ProductAddDtoValidator addDtoValidator,
        ProductUpdateDtoValidator updateDtoValidator
    )
    {
        _productService = productService;
        _addDtoValidator = addDtoValidator;
        _updateDtoValidator = updateDtoValidator;
    }

    /// <summary>
    ///     Get all products.
    /// </summary>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    /// <returns>
    ///     Returns all <see cref="ProductResponseDto"/>.
    /// </returns>
    /// <response code="200">Returns all products</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    // Admin endpoint
    public async Task<ActionResult<List<ProductResponseDto>>> GetAllAsync(
        CancellationToken ct = default
    ) => await _productService.GetAllAsync(ct);

    /// <summary>
    ///     Get all active products. Active = visible for a end user.
    /// </summary>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    /// <returns>
    ///     Returns all active <see cref="ProductResponseDto"/>.
    /// </returns>
    /// <response code="200">Returns all products.</response>
    [HttpGet("active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProductResponseDto>>> GetAllActiveAsync(
        CancellationToken ct = default
    ) => await _productService.GetAllActiveAsync(ct);

    /// <summary>
    ///     Get product for a given id.
    /// </summary>
    /// <param name="id">
    ///     The identifier of the product to retrieve.
    /// </param>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    /// <returns>
    ///     Returns product for a given <paramref name="id"/> as <see cref="ProductResponseDto"/>.
    /// </returns>
    /// <response code="200">Returns product for the given id</response>
    /// <response code="404">If product does not exist in the database.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponseDto>> GetByIdAsync(
        Guid id,
        CancellationToken ct = default
    ) => (await _productService.GetByIdAsync(id, ct)).ToActionResult<ProductResponseDto>(this);

    /// <summary>
    ///     Create new product.
    /// </summary>
    /// <param name="product">
    ///     Product to create in shape <see cref="ProductAddDto"/>
    /// </param>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    /// <returns>
    ///     Newly created <see cref="ProductResponseDto" />
    /// </returns>
    /// <response code="201">Returns the newly created product.</response>
    /// <response code="400">If the request body fails validation.</response>
    /// <response code="404">If the category with the given CategoryId does not exist.</response>
    /// <response code="409">If slug created from name is not unique.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponseDto>> AddAsync(
        ProductAddDto product,
        CancellationToken ct = default
    )
    {
        var validation = _addDtoValidator.Validate(product);
        if (!validation.IsValid)
        {
            validation.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        var result = await _productService.AddAsync(product, ct);
        return result.ToActionResult<ProductResponseDto>(this, StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Partially updates a product
    /// </summary>
    /// <param name="id">
    ///     Identifier of the product to update
    /// </param>
    /// <param name="data">
    ///     Data for updating product. All fields are optional. If no new data is provided the original data remains.
    /// </param>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    /// <returns>
    ///     Return updated <see cref="ProductResponseDto"/> for the given id.
    /// </returns>
    /// <response code="200">Returns the updated product.</response>
    /// <response code="400">If the request body fails validation.</response>
    /// <response code="404">If product with the given id, or category with the given CategoryId, does not exist.</response>
    /// <response code="409">If slug created from the new name is not unique.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponseDto>> UpdateAsync(
        Guid id,
        ProductUpdateDto data,
        CancellationToken ct = default
    )
    {
        var validation = _updateDtoValidator.Validate(data);
        if (!validation.IsValid)
        {
            validation.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        var result = await _productService.UpdateAsync(id, data, ct);
        return result.ToActionResult<ProductResponseDto>(this);
    }

    /// <summary>
    ///     Delete product for a given id.
    /// </summary>
    /// <param name="id">
    ///     The identifier of the product
    /// </param>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    /// <returns>
    ///     Returns no content
    /// </returns>
    /// <response code="204">Returns action to confirm successful operation</response>
    /// <response code="404">If product does not exists.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct = default) =>
        (await _productService.DeleteAsync(id, ct)).ToActionResult(
            this,
            StatusCodes.Status204NoContent
        );
}
