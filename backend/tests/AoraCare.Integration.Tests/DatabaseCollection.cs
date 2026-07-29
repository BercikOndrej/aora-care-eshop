namespace AoraCare.Tests.Integration;

[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<IntegrationTestFactory>
{
    public const string Name = "Database";
}
