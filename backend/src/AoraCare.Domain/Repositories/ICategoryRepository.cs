namespace AoraCare.Domain.Repositories;

/// <summary>
///     Data access abstraction for <see cref="Category"/> entities.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    ///     Gets all categories.
    /// </summary>
    Task<IReadOnlyList<Category>> GetAllCategoriesAsync(bool asNoTracking = true);

    /// <summary>
    ///     Gets all categories including their products.
    /// </summary>
    Task<IReadOnlyList<Category>> GetAllCategoriesWithProductsAsync(bool asNoTracking = true);

    /// <summary>
    ///     Gets a single category by id.
    /// </summary>
    Task<Category?> GetCategoryAsync(Guid id, bool asNoTracking = true);

    /// <summary>
    ///     Gets a single category by id including its products.
    /// </summary>
    Task<Category?> GetCategoryWithProductsAsync(Guid id, bool asNoTracking = true);

    /// <summary>
    ///     Marks a category to be inserted on the next <see cref="IUnitOfWork.SaveChangesAsync"/>.
    /// </summary>
    void Add(Category category);

    /// <summary>
    ///     Marks a category to be deleted on the next <see cref="IUnitOfWork.SaveChangesAsync"/>.
    /// </summary>
    void Remove(Category category);
}
