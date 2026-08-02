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
}
