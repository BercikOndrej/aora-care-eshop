using AoraCare.Domain.Models;

namespace AoraCare.Domain.Repositories.Interfaces;

/// <summary>
///     Data access abstraction for <see cref="Category"/> entities.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    ///     Gets all categories for read-only purposes (no change tracking).
    /// </summary>
    Task<IReadOnlyList<Category>> GetAllCategoriesAsync(CancellationToken ct = default);

    /// <summary>
    ///     Gets all categories with change tracking enabled, for callers that intend to mutate them.
    /// </summary>
    Task<IReadOnlyList<Category>> GetAllCategoriesForUpdateAsync(CancellationToken ct = default);

    /// <summary>
    ///     Gets all categories including their products, for read-only purposes (no change tracking).
    /// </summary>
    Task<IReadOnlyList<Category>> GetAllCategoriesWithProductsAsync(CancellationToken ct = default);

    /// <summary>
    ///     Gets a single category by id, for read-only purposes (no change tracking).
    /// </summary>
    Task<Category?> GetCategoryAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    ///     Gets a single category by id with change tracking enabled, for callers that intend to mutate it.
    /// </summary>
    Task<Category?> GetCategoryForUpdateAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    ///     Gets a single category by id including its products, for read-only purposes (no change tracking).
    /// </summary>
    Task<Category?> GetCategoryWithProductsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    ///     Marks a category to be inserted on the next <see cref="IUnitOfWork.SaveChangesAsync"/>.
    /// </summary>
    void Add(Category category);

    /// <summary>
    ///     Marks a category to be deleted on the next <see cref="IUnitOfWork.SaveChangesAsync"/>.
    /// </summary>
    void Remove(Category category);
}
