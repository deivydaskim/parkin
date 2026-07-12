using Microsoft.EntityFrameworkCore;
using Parkin.Api.Infrastructure.Data;
using Shouldly;
using Xunit;

namespace Parkin.IntegrationTests.Infrastructure;

public class AppDbContextConnectivityTests : IClassFixture<PostgresFixture>
{
  readonly PostgresFixture _fixture;

  public AppDbContextConnectivityTests(PostgresFixture fixture) => _fixture = fixture;

  [Fact]
  public async Task Migrates_And_Connects_To_Postgres_Container()
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseNpgsql(_fixture.ConnectionString)
      .Options;

    await using var context = new AppDbContext(options);

    await context.Database.MigrateAsync();

    (await context.Database.CanConnectAsync()).ShouldBeTrue();
  }
}
