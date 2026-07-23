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
    public async Task<IReadOnlyList<Category>> GetAllCategoriesAsync(CancellationToken ct = default) =>
        await _dbContext.Set<Category>().AsNoTracking().ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Category>> GetAllCategoriesForUpdateAsync(
        CancellationToken ct = default
    ) => await _dbContext.Set<Category>().ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Category>> GetAllCategoriesWithProductsAsync(
        CancellationToken ct = default
    ) =>
        await _dbContext
            .Set<Category>()
            .Include(c => c.Products)
            .AsNoTracking()
            .ToListAsync(ct);

    /// <inheritdoc/>
    public Task<Category?> GetCategoryAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.Set<Category>().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <inheritdoc/>
    public Task<Category?> GetCategoryForUpdateAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.Set<Category>().FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <inheritdoc/>
    public Task<Category?> GetCategoryWithProductsAsync(Guid id, CancellationToken ct = default) =>
        _dbContext
            .Set<Category>()
            .Include(c => c.Products)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <inheritdoc/>
    public void Add(Category category) => _dbContext.Set<Category>().Add(category);

    /// <inheritdoc/>
    public void Remove(Category category) => _dbContext.Set<Category>().Remove(category);
}
