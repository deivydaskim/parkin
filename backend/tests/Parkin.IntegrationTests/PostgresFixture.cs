using Testcontainers.PostgreSql;
using Xunit;

namespace Parkin.IntegrationTests;

public sealed class PostgresFixture : IAsyncLifetime
{
  readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
    .WithImage("postgres:16-alpine")
    .WithDatabase("parkin_integration_tests")
    .WithUsername("postgres")
    .WithPassword("postgres")
    .Build();

  public string ConnectionString => _container.GetConnectionString();

  public Task InitializeAsync() => _container.StartAsync();

  public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
