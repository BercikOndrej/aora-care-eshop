using AoraCare.Domain.Models;
using AoraCare.Domain.Repositories.Interfaces;
using AoraCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoraCare.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _dbContext;

    public CategoryRepository(AppDbContext context)
    {
        _dbContext = context;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default) =>
        await _dbContext.Set<Category>().AsNoTracking().ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Category>> GetAllForUpdateAsync(
        CancellationToken ct = default
    ) => await _dbContext.Set<Category>().ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Category>> GetAllCategoriesWithProductsAsync(
        CancellationToken ct = default
    ) => await _dbContext.Set<Category>().Include(c => c.Products).AsNoTracking().ToListAsync(ct);

    /// <inheritdoc/>
    public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.Set<Category>().FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <inheritdoc/>
    public Task<Category?> GetCategoryWithProductsByIdAsync(
        Guid id,
        CancellationToken ct = default
    ) =>
        _dbContext
            .Set<Category>()
            .Include(c => c.Products)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <inheritdoc/>
    public void Add(Category category) => _dbContext.Set<Category>().Add(category);

    /// <inheritdoc/>
    public void Remove(Category category) => _dbContext.Set<Category>().Remove(category);

    /// <inheritdoc/>
    public Task<bool> SlugExistsAsync(
        string slug,
        Guid? excludeId = null,
        CancellationToken ct = default
    ) => _dbContext.Set<Category>().AnyAsync(c => c.Slug == slug && c.Id != excludeId, ct);

    /// <inheritdoc/>
    public Task<int> CountAsync(CancellationToken ct = default) =>
        _dbContext.Set<Category>().CountAsync(ct);
}
