using AoraCare.Domain;
using AoraCare.Infrastructure.Data;

namespace AoraCare.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext context)
    {
        _db = context;
    }

    /// <inheritdoc/>
    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
