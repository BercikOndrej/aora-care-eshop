using AoraCare.Application.Dtos;
using ErrorOr;

namespace AoraCare.Application.Services.Interfaces;

public interface IProductService
{
    /// <summary>
    ///     Provides all products.
    /// </summary>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    /// <returns>
    ///     Returns collection of all products.
    /// </returns>
    Task<List<ProductResponseDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    ///     Provides only active products, for public/storefront consumption.
    /// </summary>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    /// <returns>
    ///     Returns collection of active products.
    /// </returns>
    Task<List<ProductResponseDto>> GetAllActiveAsync(CancellationToken ct = default);

    /// <summary>
    ///     Get product for a given id.
    /// </summary>
    /// <param name="id">
    ///     The identifier of the product.
    /// </param>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    /// <returns>
    ///     Returns an <see cref="ErrorOr{TValue}"/> containing the <see cref="ProductResponseDto"/> if it exists,
    ///     or an <see cref="Error"/> of type <see cref="ErrorType.NotFound"/> if no category with the given <paramref name="id"/> exists.
    /// </returns>
    Task<ErrorOr<ProductResponseDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    ///     Create new Product.
    ///     SortOrder = Count products in one category + 1 and new created product is active by default.
    /// </summary>
    /// <param name="category">
    ///     <see cref="ProductAddDto"/> to create category.
    /// </param>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    /// <returns>
    ///     Returns an <see cref="ErrorOr{TValue}"/> containing the newly created <see cref="ProductResponseDto"/>,
    ///     or an <see cref="Error"/> of type <see cref="ErrorType.Conflict"/> if the slug derived from the name is already taken.
    /// </returns>
    Task<ErrorOr<ProductResponseDto>> AddAsync(
        ProductAddDto product,
        CancellationToken ct = default
    );

    /// <summary>
    ///     Update product for a given id.
    /// </summary>
    /// <param name="id">
    ///     Identifier of the product to update.
    /// </param>
    /// <param name="data">
    ///     Data to update product.
    /// </param>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    /// <returns>
    ///     Returns an <see cref="ErrorOr{TValue}"/> containing the updated <see cref="ProductResponseDto"/>,
    ///     an <see cref="Error"/> of type <see cref="ErrorType.NotFound"/> if no product with the given <paramref name="id"/> exists,
    ///     an <see cref="Error"/> of type <see cref="ErrorType.Validation"/> if <paramref name="data"/> contains an out-of-range SortOrder,
    ///     or an <see cref="Error"/> of type <see cref="ErrorType.Conflict"/> if the slug derived from the new name is already taken.
    /// </returns>
    Task<ErrorOr<ProductResponseDto>> UpdateAsync(
        Guid id,
        ProductUpdateDto data,
        CancellationToken ct = default
    );

    /// <summary>
    ///     Delete product for a given id.
    /// </summary>
    /// <param name="id">
    ///     The identifier of product to remove.
    /// </param>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    /// <returns>
    ///     Returns an <see cref="ErrorOr{TValue}"/> of <see cref="Deleted"/> if the product was deleted,
    ///     or an <see cref="Error"/> of type <see cref="ErrorType.NotFound"/> if no product with the given <paramref name="id"/> exists.
    /// </returns>
    Task<ErrorOr<Deleted>> DeleteAsync(Guid id, CancellationToken ct = default);
}
