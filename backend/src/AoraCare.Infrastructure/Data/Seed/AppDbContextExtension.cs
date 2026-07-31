using Microsoft.EntityFrameworkCore;

namespace AoraCare.Infrastructure.Data.Seed;

public static class AppDbContextExtension
{
    public static DbContextOptionsBuilder UseAppSeeding(this DbContextOptionsBuilder options)
    {
        options.UseSeeding(
            (context, _) =>
                DbSeeder
                    .SeedAsync((AppDbContext)context, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult()
        );
        options.UseAsyncSeeding(
            async (context, _, ct) => await DbSeeder.SeedAsync((AppDbContext)context, ct)
        );
        return options;
    }
}
