using Microsoft.EntityFrameworkCore;

namespace AoraCare.Api.Data;

/// <summary>
/// EF Core database context for the AoraCare application.
/// Entities are never configured here directly — use IEntityTypeConfiguration&lt;T&gt;
/// classes placed in this assembly; they are picked up automatically.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Discovers and applies all IEntityTypeConfiguration<T> implementations
        // in this assembly, keeping entity config out of this class.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
