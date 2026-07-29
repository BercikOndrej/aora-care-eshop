using AoraCare.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace AoraCare.Tests.Integration;

public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly HttpClient client;
    private readonly IntegrationTestFactory _factory;

    protected BaseIntegrationTest(IntegrationTestFactory factory)
    {
        _factory = factory;
        client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabase();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    ///     Seeds data directly via <see cref="AppDbContext"/>, for entities that have no API to create them through yet.
    /// </summary>
    protected async Task SeedAsync(Action<AppDbContext> seed)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        seed(db);
        await db.SaveChangesAsync();
    }
}
