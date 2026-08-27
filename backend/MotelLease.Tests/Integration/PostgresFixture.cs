using Testcontainers.PostgreSql;

namespace MotelLease.Tests.Integration;

/// <summary>
/// One PostGIS container for the whole test run. The plain postgres image cannot host this
/// schema — BoardingHouses has a geography column (CLAUDE.md, Testing).
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgis/postgis:17-3.5")
        .WithDatabase("motellease")
        .WithUsername("motellease")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
