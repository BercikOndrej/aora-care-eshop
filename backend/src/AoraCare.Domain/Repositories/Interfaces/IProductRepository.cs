using AoraCare.Domain.Models;

namespace AoraCare.Domain.Repositories.Interfaces;

public interface IProductRepository : IModelRepository<Product>
{
    public Task<List<Product>> GetAllProductsInCategory(
        Guid categoryId,
        CancellationToken ct = default
    );

    /// <summary>
    ///     Checks whether a product with the given slug already exists.
    /// </summary>
    /// <param name="slug">
    ///     The slug to check.
    /// </param>
    /// <param name="excludeId">
    ///     An id to exclude from the check, for update scenarios where the entity being
    ///     updated already owns the slug.
    /// </param>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    Task<bool> SlugExistsAsync(
        string slug,
        Guid? excludeId = null,
        CancellationToken ct = default
    );

    /// <summary>
    ///     Counts products in the given category. Since <see cref="Product.SortOrder"/>
    ///     is a contiguous 0-based sequence per category, this value is also the next
    ///     valid sort order for a product appended to the category.
    /// </summary>
    /// <param name="categoryId">
    ///     The category to count products in.
    /// </param>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    Task<int> CountInCategoryAsync(Guid categoryId, CancellationToken ct = default);
}
