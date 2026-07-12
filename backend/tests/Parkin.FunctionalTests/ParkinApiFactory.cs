using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Xunit;

namespace Parkin.FunctionalTests;

public sealed class ParkinApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
  readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
    .WithImage("postgres:16-alpine")
    .WithDatabase("parkin_functional_tests")
    .WithUsername("postgres")
    .WithPassword("postgres")
    .Build();

  public async Task InitializeAsync() => await _container.StartAsync();

  public new async Task DisposeAsync()
  {
    await _container.DisposeAsync();
    await base.DisposeAsync();
  }

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment("Development");

    builder.ConfigureAppConfiguration((_, config) =>
    {
      config.AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["ConnectionStrings:AppDb"] = _container.GetConnectionString(),
      });
    });
  }
}
