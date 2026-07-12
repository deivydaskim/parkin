using System.Net;
using Shouldly;
using Xunit;

namespace Parkin.FunctionalTests;

public class HealthCheckTests : IClassFixture<ParkinApiFactory>
{
  readonly ParkinApiFactory _factory;

  public HealthCheckTests(ParkinApiFactory factory) => _factory = factory;

  [Fact]
  public async Task Host_Boots_Against_Real_Postgres_And_Reports_Healthy()
  {
    using var client = _factory.CreateClient();

    var response = await client.GetAsync("/health");

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
  }
}
