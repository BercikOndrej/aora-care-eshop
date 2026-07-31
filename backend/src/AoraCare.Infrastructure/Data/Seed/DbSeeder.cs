using AoraCare.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AoraCare.Infrastructure.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (!await db.Categories.AnyAsync(ct))
        {
            // Seed logic
            await db.SaveChangesAsync(ct);
        }
    }
}
