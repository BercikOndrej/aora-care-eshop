using AoraCare.Domain.Models;
using AoraCare.Domain.Repositories.Interfaces;
using AoraCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoraCare.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;

    public ProductRepository(AppDbContext context)
    {
        _db = context;
    }

    public async Task<List<Product>> GetAllProductsInCategory(
        Guid categoryId,
        CancellationToken ct = default
    ) =>
        await _db.Set<Product>()
            .Where(p => p.CategoryId == categoryId)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(ct);

    public void Add(Product entity) => _db.Set<Product>().Add(entity);

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Set<Product>().AsNoTracking().Include(p => p.Category).ToListAsync(ct);

    public async Task<IReadOnlyList<Product>> GetAllForUpdateAsync(
        CancellationToken ct = default
    ) => await _db.Set<Product>().Include(p => p.Category).ToListAsync(ct);

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Set<Product>().Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id, ct);

    public void Remove(Product entity) => _db.Set<Product>().Remove(entity);

    public Task<bool> SlugExistsAsync(
        string slug,
        Guid? excludeId = null,
        CancellationToken ct = default
    ) => _db.Set<Product>().AnyAsync(p => p.Slug == slug && p.Id != excludeId, ct);

    public Task<int> CountInCategoryAsync(Guid categoryId, CancellationToken ct = default) =>
        _db.Set<Product>().CountAsync(p => p.CategoryId == categoryId, ct);
}
