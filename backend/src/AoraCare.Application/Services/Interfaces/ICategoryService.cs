using AoraCare.Application.Dtos;
using ErrorOr;

namespace AoraCare.Application.Services.Interfaces;

public interface ICategoryService
{
    /// <summary>
    ///     Provides all categories
    /// </summary>
    /// <returns>
    ///     Returns collection of all Categories.
    /// </returns>
    Task<List<CategoryResponseDto>> GetAllAsync();

    /// <summary>
    ///     Provides only active categories, for public/storefront consumption.
    /// </summary>
    /// <returns>
    ///     Returns collection of active Categories.
    /// </returns>
    Task<List<CategoryResponseDto>> GetAllActiveAsync();

    /// <summary>
    ///     Get category for a given id.
    /// </summary>
    /// <param name="id">
    ///     The identifier of the category.
    /// </param>
    /// <returns>
    ///     Returns an <see cref="ErrorOr{TValue}"/> containing the <see cref="CategoryResponseDto"/> if it exists,
    ///     or an <see cref="Error"/> of type <see cref="ErrorType.NotFound"/> if no category with the given <paramref name="id"/> exists.
    /// </returns>
    Task<ErrorOr<CategoryResponseDto>> GetAsync(Guid id);

    /// <summary>
    ///     Create new Category.
    ///     SortOrder = Max + 1 and new created category is active by default.
    /// </summary>
    /// <param name="category">
    ///     <see cref="CategoryAddDto"/> to create category.
    /// </param>
    /// <returns>
    ///     Returns an <see cref="ErrorOr{TValue}"/> containing the newly created <see cref="CategoryResponseDto"/>,
    ///     or an <see cref="Error"/> of type <see cref="ErrorType.Conflict"/> if the slug derived from the name is already taken.
    /// </returns>
    Task<ErrorOr<CategoryResponseDto>> AddAsync(CategoryAddDto category);

    /// <summary>
    ///     Update category for a given id.
    /// </summary>
    /// <param name="id">
    ///     Identifier of the category to update.
    /// </param>
    /// <param name="data">
    ///     Data to update category.
    /// </param>
    /// <returns>
    ///     Returns an <see cref="ErrorOr{TValue}"/> containing the updated <see cref="CategoryResponseDto"/>,
    ///     an <see cref="Error"/> of type <see cref="ErrorType.NotFound"/> if no category with the given <paramref name="id"/> exists,
    ///     an <see cref="Error"/> of type <see cref="ErrorType.Validation"/> if <paramref name="data"/> contains an out-of-range SortOrder,
    ///     or an <see cref="Error"/> of type <see cref="ErrorType.Conflict"/> if the slug derived from the new name is already taken.
    /// </returns>
    Task<ErrorOr<CategoryResponseDto>> UpdateAsync(Guid id, CategoryUpdateDto data);

    /// <summary>
    ///     Delete category for a given id.
    /// </summary>
    /// <param name="id">
    ///     The identifier of category to remove.
    /// </param>
    /// <returns>
    ///     Returns an <see cref="ErrorOr{TValue}"/> of <see cref="Deleted"/> if the category was deleted,
    ///     or an <see cref="Error"/> of type <see cref="ErrorType.NotFound"/> if no category with the given <paramref name="id"/> exists.
    /// </returns>
    Task<ErrorOr<Deleted>> DeleteAsync(Guid id);
}
