using AoraCare.Application.Dtos;
using AoraCare.Application.Services.Interfaces;
using AoraCare.Domain;
using AoraCare.Domain.Common;
using AoraCare.Infrastructure.Data;
using ErrorOr;
using Microsoft.EntityFrameworkCore;

namespace AoraCare.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;

    public CategoryService(AppDbContext db) => _db = db;

    /// <inheritdoc cref="ICategoryService.GetAllAsync"/>
    /// <remarks>
    ///     For admin use — includes inactive categories.
    /// </remarks>
    public async Task<List<CategoryResponseDto>> GetAllAsync() =>
        await _db
            .Categories.AsNoTracking()
            .Select(c => new CategoryResponseDto(
                c.Id,
                c.Name,
                c.Slug,
                c.Description,
                c.SortOrder,
                c.IsActive
            ))
            .ToListAsync();

    /// <inheritdoc cref="ICategoryService.GetAllActiveAsync"/>
    public async Task<List<CategoryResponseDto>> GetAllActiveAsync() =>
        await _db
            .Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new CategoryResponseDto(
                c.Id,
                c.Name,
                c.Slug,
                c.Description,
                c.SortOrder,
                c.IsActive
            ))
            .ToListAsync();

    /// <inheritdoc cref="ICategoryService.GetAsync"/>
    public async Task<ErrorOr<CategoryResponseDto>> GetAsync(Guid id)
    {
        var category = await _db
            .Categories.AsNoTracking()
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);
        return category is null
            ? Error.NotFound(description: $"Category with {id} not found.")
            : category.ToDto();
    }

    /// <inheritdoc cref="ICategoryService.AddAsync"/>
    public async Task<ErrorOr<CategoryResponseDto>> AddAsync(CategoryAddDto dto)
    {
        string slug = SlugHelper.CreateSlug(dto.Name);
        if (!await IsSlugUnique(slug))
            return Error.Conflict(
                description: $"Property {nameof(dto.Name)} has no unique slug. Try another name (Slug is created form name automatically)"
            );

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Products = [],
            Name = dto.Name,
            Slug = slug,
            Description = dto.Description,
            SortOrder = await GetNextOrder(),
            IsActive = dto.IsActive ?? true,
            CreatedAt = DateTime.UtcNow,
        };
        await _db.Categories.AddAsync(category);
        await _db.SaveChangesAsync();
        return category.ToDto();
    }

    /// <inheritdoc cref="ICategoryService.UpdateAsync"/>
    public async Task<ErrorOr<CategoryResponseDto>> UpdateAsync(Guid id, CategoryUpdateDto dto)
    {
        var old = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (old is null)
            return Error.NotFound(description: $"Category with {id} not found.");

        if (dto.Name is not null && !dto.Name.Equals(old.Name))
        {
            var slug = SlugHelper.CreateSlug(dto.Name);
            if (!await IsSlugUnique(slug, id))
                return Error.Conflict(
                    description: $"Property {nameof(dto.Name)} has no unique slug. Try another name."
                );

            old.Name = dto.Name;
            old.Slug = slug;
        }

        old.Description = dto.Description ?? old.Description;
        old.IsActive = dto.IsActive ?? old.IsActive;

        if (dto.SortOrder is not null)
        {
            int newIndex = dto.SortOrder.Value;
            if (newIndex < 0 || newIndex >= await _db.Categories.AsNoTracking().CountAsync())
                return Error.Validation(
                    description: $"SortOrder value is out of index range. Value cannot be greater than categories count"
                );
            await UpdateCategoryOrder(id, dto.SortOrder.Value);
        }

        await _db.SaveChangesAsync();
        return old.ToDto();
    }

    /// <inheritdoc cref="ICategoryService.DeleteAsync"/>
    public async Task<ErrorOr<Deleted>> DeleteAsync(Guid id)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
            return Error.NotFound(description: $"Category with {id} not found.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        await ReorderCategoryOrderAfterDelete();
        await _db.SaveChangesAsync();

        return Result.Deleted;
    }

    #region private methods

    /// <summary>
    ///     Get next valid value for correct sorting property.
    /// </summary>
    /// <returns>
    ///     Value that represent postition.
    /// </returns>
    private async Task<int> GetNextOrder() =>
        await _db.Categories.MaxAsync(c => (int?)c.SortOrder) is int max ? max + 1 : 0;

    /// <summary>
    ///     Helper method to get all ordered categories.
    /// </summary>
    /// <returns>
    ///     List of ordered categories by property SortOrder
    /// </returns>
    private async Task<List<Category>> GetOrderedCategoriesAsync() =>
        await _db.Categories.OrderBy(c => c.SortOrder).ToListAsync();

    /// <summary>
    ///     Reorder all categories based on order in given list.
    /// </summary>
    /// <param name="categories"></param>
    private void ReorderAllCategories(List<Category> categories)
    {
        for (int i = 0; i < categories.Count; i++)
            categories[i].SortOrder = i;
    }

    /// <summary>
    ///     Set new position to given category. Positions of other categories are also changed.
    /// </summary>
    /// <param name="id">
    ///     The identifier of category to move.
    /// </param>
    /// <param name="newIndex">
    ///     New position for the given category. Allowed values have to be in interval [0, category count).
    /// </param>
    private async Task UpdateCategoryOrder(Guid id, int newIndex)
    {
        var categories = await GetOrderedCategoriesAsync();

        var found = categories.First(c => c.Id == id);

        categories.Remove(found);
        categories.Insert(newIndex, found);

        ReorderAllCategories(categories);
    }

    /// <summary>
    ///     Fix order of all categories after successful delete of one category.
    /// </summary>
    /// <returns></returns>
    private async Task ReorderCategoryOrderAfterDelete()
    {
        var categories = await GetOrderedCategoriesAsync();
        ReorderAllCategories(categories);
    }

    /// <summary>
    ///     Test if given slug is unique and also it isn't the same object which is being updated.
    /// </summary>
    /// <param name="slug">
    ///     Slug that is tested.
    /// </param>
    /// <param name="id">
    ///     Id of updated item.
    /// </param>
    /// <returns></returns>
    private async Task<bool> IsSlugUnique(string slug, Guid? id = null) =>
        !await _db.Categories.AnyAsync(c => c.Slug == slug && c.Id != id);

    #endregion
}
