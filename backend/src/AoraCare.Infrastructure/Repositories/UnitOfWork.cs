using AoraCare.Domain.Repositories;
using AoraCare.Infrastructure.Data;

namespace AoraCare.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext context)
    {
        _db = context;
    }

    /// <inheritdoc/>
    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
