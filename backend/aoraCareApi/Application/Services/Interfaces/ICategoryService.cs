using aoraCareApi.Application.Dtos;
using aoraCareApi.Domain;
using Microsoft.AspNetCore.Mvc;

namespace aoraCareApi.Application.Services.Interfaces;

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
    ///     Returns category if exists. If category does not exist, a null value is returned.
    /// </returns>
    Task<CategoryResponseDto?> GetAsync(Guid id);

    /// <summary>
    ///     Create new Category.
    ///     SortOrder = Max + 1 and new created category is active by default.
    /// </summary>
    /// <param name="category">
    ///     <see cref="CategoryAddDto"/> to create category.
    /// </param>
    /// <returns>
    ///     Returns newly created category.
    /// </returns>
    Task<CategoryResponseDto> AddAsync(CategoryAddDto category);

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
    ///     Returns null if category is not found. If it is, returns updated category.
    /// </returns>
    Task<CategoryResponseDto?> UpdateAsync(Guid id, CategoryUpdateDto data);

    /// <summary>
    ///     Delete category for a given id.
    /// </summary>
    /// <param name="id">
    ///     The identifier of category to remove.
    /// </param>
    /// <returns>
    ///     Returns true if category is successfully deleted. False if category does not exist.
    /// </returns>
    Task<bool> DeleteAsync(Guid id);
}
