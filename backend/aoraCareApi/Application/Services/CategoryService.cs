using aoraCareApi.Application.Dtos;
using aoraCareApi.Application.Services.Interfaces;
using aoraCareApi.Domain;
using aoraCareApi.Domain.Common;
using aoraCareApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace aoraCareApi.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;

    public CategoryService(AppDbContext db) => _db = db;

    /// <inheritdoc cref="ICategoryService.GetAllAsync"/>
    /// For admin
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
    public async Task<CategoryResponseDto?> GetAsync(Guid id)
    {
        var category = await _db
            .Categories.AsNoTracking()
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);
        return category is null ? null : category.ToDto();
    }

    /// <inheritdoc cref="ICategoryService.AddAsync"/>
    public async Task<CategoryResponseDto> AddAsync(CategoryAddDto dto)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Products = [],
            Name = dto.Name,
            Slug = SlugHelper.CreateSlug(dto.Name),
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
    public async Task<CategoryResponseDto?> UpdateAsync(Guid id, CategoryUpdateDto dto)
    {
        var old = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);

        if (old is null)
            return null;

        old.Name = dto.Name ?? old.Name;
        old.Slug = dto.Name is null ? old.Slug : SlugHelper.CreateSlug(dto.Name);
        old.Description = dto.Description ?? old.Description;
        old.IsActive = dto.IsActive ?? old.IsActive;

        if (dto.SortOrder is not null)
            await ReorderCategory(id, dto.SortOrder.Value);
        else
            await _db.SaveChangesAsync();
        return old.ToDto();
    }

    /// <inheritdoc cref="ICategoryService.DeleteAsync"/>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
            return false;

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return true;
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
    ///     Set new position to given category. Positions of other categories are also changed.
    /// </summary>
    /// <param name="id">
    ///     The identifier of category to move.
    /// </param>
    /// <param name="newIndex">
    ///     New position for the given category. Allowed values have to be in interval [0, category count).
    /// </param>
    /// <exception cref="ArgumentException">
    ///     Thrown when no category with the given <paramref name="id"/> exists.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="newIndex"/> is outside the valid range.
    /// </exception>
    private async Task ReorderCategory(Guid id, int newIndex)
    {
        var categories = await _db.Categories.OrderBy(c => c.SortOrder).ToListAsync();
        var found = categories.FirstOrDefault(c => c.Id == id);
        if (found is null)
            throw new ArgumentException($"Category with {id} not found");

        if (newIndex < 0 || newIndex >= categories.Count)
            throw new ArgumentOutOfRangeException(nameof(newIndex));

        categories.Remove(found);
        categories.Insert(newIndex, found);

        for (int i = 0; i < categories.Count; i++)
            categories[i].SortOrder = i;

        await _db.SaveChangesAsync();
    }

    #endregion
}
