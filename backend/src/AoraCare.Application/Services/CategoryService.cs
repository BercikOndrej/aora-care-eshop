using AoraCare.Application.Dtos;
using AoraCare.Application.Services.Interfaces;
using AoraCare.Domain;
using AoraCare.Domain.Common;
using AoraCare.Domain.Models;
using AoraCare.Domain.Repositories.Interfaces;
using ErrorOr;

namespace AoraCare.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;
    private readonly IUnitOfWork _uow;

    public CategoryService(ICategoryRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _uow = unitOfWork;
    }

    /// <inheritdoc cref="ICategoryService.GetAllAsync"/>
    /// <remarks>
    ///     For admin use — includes inactive categories.
    /// </remarks>
    public async Task<List<CategoryResponseDto>> GetAllAsync(CancellationToken ct = default) =>
        (await _repository.GetAllAsync(ct: ct))
            .Select(c => new CategoryResponseDto(
                c.Id,
                c.Name,
                c.Slug,
                c.Description,
                c.SortOrder,
                c.IsActive
            ))
            .ToList();

    /// <inheritdoc cref="ICategoryService.GetAllActiveAsync"/>
    public async Task<List<CategoryResponseDto>> GetAllActiveAsync(
        CancellationToken ct = default
    ) =>
        (await _repository.GetAllAsync(ct: ct))
            .Where(c => c.IsActive)
            .Select(c => new CategoryResponseDto(
                c.Id,
                c.Name,
                c.Slug,
                c.Description,
                c.SortOrder,
                c.IsActive
            ))
            .ToList();

    /// <inheritdoc cref="ICategoryService.GetAsync"/>
    public async Task<ErrorOr<CategoryResponseDto>> GetAsync(
        Guid id,
        CancellationToken ct = default
    )
    {
        var category = await _repository.GetCategoryWithProductsAsync(id, ct: ct);

        return category is null
            ? Error.NotFound(description: $"Category with {id} not found.")
            : category.ToDto();
    }

    /// <inheritdoc cref="ICategoryService.AddAsync"/>
    public async Task<ErrorOr<CategoryResponseDto>> AddAsync(
        CategoryAddDto dto,
        CancellationToken ct = default
    )
    {
        string slug = SlugHelper.CreateSlug(dto.Name);
        if (!await IsSlugUnique(slug, ct: ct))
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
            SortOrder = await GetNextOrder(ct),
            IsActive = dto.IsActive ?? true,
            CreatedAt = DateTime.UtcNow,
        };
        _repository.Add(category);
        await _uow.SaveChangesAsync(ct);
        return category.ToDto();
    }

    /// <inheritdoc cref="ICategoryService.UpdateAsync"/>
    public async Task<ErrorOr<CategoryResponseDto>> UpdateAsync(
        Guid id,
        CategoryUpdateDto dto,
        CancellationToken ct = default
    )
    {
        var old = await _repository.GetByIdForUpdateAsync(id, ct);
        if (old is null)
            return Error.NotFound(description: $"Category with {id} not found.");

        if (dto.Name is not null && !dto.Name.Equals(old.Name))
        {
            var slug = SlugHelper.CreateSlug(dto.Name);
            if (!await IsSlugUnique(slug, id, ct))
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
            if (newIndex < 0 || newIndex >= (await _repository.GetAllAsync(ct: ct)).Count)
                return Error.Validation(
                    description: $"SortOrder value is out of index range. Value cannot be greater than categories count"
                );
            await UpdateCategoryOrder(id, dto.SortOrder.Value, ct);
        }

        await _uow.SaveChangesAsync(ct);
        return old.ToDto();
    }

    /// <inheritdoc cref="ICategoryService.DeleteAsync"/>
    public async Task<ErrorOr<Deleted>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var categories = await GetOrderedCategoriesAsync(ct);
        var category = categories.FirstOrDefault(c => c.Id == id);

        if (category is null)
            return Error.NotFound(description: $"Category with {id} not found.");

        _repository.Remove(category);
        categories.Remove(category);
        ReorderAllCategories(categories);

        await _uow.SaveChangesAsync(ct);

        return Result.Deleted;
    }

    #region private methods

    /// <summary>
    ///     Get next valid value for correct sorting property.
    /// </summary>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    /// <returns>
    ///     Value that represent postition.
    /// </returns>
    private async Task<int> GetNextOrder(CancellationToken ct = default) =>
        (await _repository.GetAllAsync(ct: ct)).Max(c => (int?)c.SortOrder) is int max
            ? max + 1
            : 0;

    /// <summary>
    ///     Helper method to get all ordered categories.
    ///     Entities are tracked since callers mutate <see cref="Category.SortOrder"/> in place.
    /// </summary>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    /// <returns>
    ///     List of ordered categories by property SortOrder
    /// </returns>
    private async Task<List<Category>> GetOrderedCategoriesAsync(CancellationToken ct = default) =>
        (await _repository.GetAllForUpdateAsync(ct)).OrderBy(c => c.SortOrder).ToList();

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
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    private async Task UpdateCategoryOrder(Guid id, int newIndex, CancellationToken ct = default)
    {
        var categories = await GetOrderedCategoriesAsync(ct: ct);

        var found = categories.First(c => c.Id == id);

        categories.Remove(found);
        categories.Insert(newIndex, found);

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
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    /// <returns></returns>
    private async Task<bool> IsSlugUnique(
        string slug,
        Guid? id = null,
        CancellationToken ct = default
    ) => !(await _repository.GetAllAsync(ct: ct)).Any(c => c.Slug == slug && c.Id != id);

    #endregion
}
