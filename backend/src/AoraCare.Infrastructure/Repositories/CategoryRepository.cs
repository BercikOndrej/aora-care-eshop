using AoraCare.Domain.Models;
using AoraCare.Domain.Repositories;
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
    public async Task<IReadOnlyList<Category>> GetAllCategoriesAsync(bool asNoTracking = true)
    {
        IQueryable<Category> query = _dbContext.Set<Category>();
        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Category>> GetAllCategoriesWithProductsAsync(
        bool asNoTracking = true
    )
    {
        IQueryable<Category> query = _dbContext.Set<Category>().Include(c => c.Products);
        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.ToListAsync();
    }

    /// <inheritdoc/>
    public Task<Category?> GetCategoryAsync(Guid id, bool asNoTracking = true)
    {
        IQueryable<Category> query = _dbContext.Set<Category>();
        if (asNoTracking)
            query = query.AsNoTracking();

        return query.FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <inheritdoc/>
    public Task<Category?> GetCategoryWithProductsAsync(Guid id, bool asNoTracking = true)
    {
        IQueryable<Category> query = _dbContext.Set<Category>().Include(c => c.Products);
        if (asNoTracking)
            query = query.AsNoTracking();

        return query.FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <inheritdoc/>
    public void Add(Category category) => _dbContext.Set<Category>().Add(category);

    /// <inheritdoc/>
    public void Remove(Category category) => _dbContext.Set<Category>().Remove(category);
}
