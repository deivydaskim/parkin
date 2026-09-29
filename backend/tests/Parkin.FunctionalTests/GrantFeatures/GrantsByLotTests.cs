using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Parkin.Api.Domain.AccessGrantAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Features.Drivers;
using Parkin.Api.Features.Grants;
using Parkin.Api.Features.Grants.List;
using Parkin.Api.Features.Lots;
using Parkin.Api.Features.Lots.Create;
using Shouldly;
using Xunit;

namespace Parkin.FunctionalTests.GrantFeatures;

public class GrantsByLotTests : IClassFixture<ParkinApiFactory>
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
  {
    Converters = { new JsonStringEnumConverter() },
  };

  private readonly ParkinApiFactory _factory;

  public GrantsByLotTests(ParkinApiFactory factory) => _factory = factory;

  private static async Task LoginAsOperatorAsync(HttpClient client)
  {
    var response = await client.PostAsJsonAsync("/auth/login",
      new { Email = "operator@parkin.local", Password = "Operator!2345" });
    response.EnsureSuccessStatusCode();
  }

  private static async Task<Guid> CreateRestrictedLotAsync(HttpClient client)
  {
    var response = await client.PostAsJsonAsync("/lots", new CreateLotRequest
    {
      Name = $"Grants Lot {Guid.NewGuid():N}",
      Timezone = "Europe/Vilnius",
      AccessMode = AccessMode.Restricted,
    });
    response.EnsureSuccessStatusCode();
    var lot = await response.Content.ReadFromJsonAsync<LotRecord>(JsonOptions);
    lot.ShouldNotBeNull();
    return lot.Id;
  }

  private static async Task<DriverRecord> CreateDriverAsync(HttpClient client)
  {
    var response = await client.PostAsJsonAsync("/drivers", new { Name = $"Driver {Guid.NewGuid():N}" });
    response.EnsureSuccessStatusCode();
    var driver = await response.Content.ReadFromJsonAsync<DriverRecord>(JsonOptions);
    driver.ShouldNotBeNull();
    return driver;
  }

  private static async Task<GrantRecord> CreateGrantAsync(HttpClient client, Guid driverId, Guid lotId)
  {
    var response = await client.PostAsJsonAsync("/grants", new { DriverId = driverId, LotId = lotId });
    response.StatusCode.ShouldBe(HttpStatusCode.Created);
    var grant = await response.Content.ReadFromJsonAsync<GrantRecord>(JsonOptions);
    grant.ShouldNotBeNull();
    return grant;
  }

  private static async Task<GrantListResponse> ListByLotAsync(HttpClient client, Guid lotId)
  {
    var result = await client.GetFromJsonAsync<GrantListResponse>($"/lots/{lotId}/grants", JsonOptions);
    result.ShouldNotBeNull();
    return result;
  }

  [Fact]
  public async Task Get_Unauthenticated_Returns401()
  {
    using var client = _factory.CreateClient();

    var response = await client.GetAsync($"/lots/{Guid.NewGuid()}/grants");

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Get_ListsGrantWithDriverName()
  {
    using var client = _factory.CreateClient();
    await LoginAsOperatorAsync(client);
    var lotId = await CreateRestrictedLotAsync(client);
    var driver = await CreateDriverAsync(client);
    var grant = await CreateGrantAsync(client, driver.Id, lotId);

    var result = await ListByLotAsync(client, lotId);

    var listed = result.Items.ShouldHaveSingleItem();
    listed.Id.ShouldBe(grant.Id);
    listed.DriverId.ShouldBe(driver.Id);
    listed.DriverName.ShouldBe(driver.Name);
    listed.Status.ShouldBe(GrantStatus.Active);
  }

  [Fact]
  public async Task Get_RevokedGrant_StillListedWithStatus()
  {
    using var client = _factory.CreateClient();
    await LoginAsOperatorAsync(client);
    var lotId = await CreateRestrictedLotAsync(client);
    var driver = await CreateDriverAsync(client);
    var grant = await CreateGrantAsync(client, driver.Id, lotId);
    (await client.PostAsync($"/grants/{grant.Id}/revoke", null)).EnsureSuccessStatusCode();

    var result = await ListByLotAsync(client, lotId);

    var listed = result.Items.ShouldHaveSingleItem();
    listed.Status.ShouldBe(GrantStatus.Revoked);
    listed.DriverName.ShouldBe(driver.Name);
  }
}
