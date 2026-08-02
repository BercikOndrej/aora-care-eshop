using AoraCare.Domain.Models;

namespace AoraCare.Domain.Repositories.Interfaces;

/// <summary>
///     Data access abstraction for <see cref="Category"/> entities.
/// </summary>
public interface ICategoryRepository : IModelRepository<Category>
{
    /// <summary>
    ///     Gets all categories including their products, for read-only purposes (no change tracking).
    /// </summary>
    Task<IReadOnlyList<Category>> GetAllCategoriesWithProductsAsync(CancellationToken ct = default);

    /// <summary>
    ///     Gets a single category by id including its products, for read-only purposes (no change tracking).
    /// </summary>
    Task<Category?> GetCategoryWithProductsByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    ///     Checks whether a category with the given slug already exists.
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
    ///     Counts all categories. Since <see cref="Category.SortOrder"/> is a contiguous
    ///     0-based sequence, this value is also the next valid sort order for a category
    ///     appended at the end.
    /// </summary>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    Task<int> CountAsync(CancellationToken ct = default);
}
