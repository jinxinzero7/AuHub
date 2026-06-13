using Testcontainers.PostgreSql;

namespace IntegrationTestSupport;

public sealed class PostgresTestDatabase : IAsyncDisposable
{
    private readonly PostgreSqlContainer _container;

    public PostgresTestDatabase(string databaseName)
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase(databaseName)
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public Task StartAsync()
    {
        return _container.StartAsync();
    }

    public ValueTask DisposeAsync()
    {
        return _container.DisposeAsync();
    }
}
