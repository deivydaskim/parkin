using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.DriverFeatures;
using Parkin.Api.LotFeatures;
using Parkin.Api.LotFeatures.Create;
using Parkin.Api.SessionFeatures.ListActiveByLot;
using Shouldly;
using Xunit;

namespace Parkin.FunctionalTests.SessionFeatures;

public class ActiveSessionsByLotTests : IClassFixture<ParkinApiFactory>
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
  {
    Converters = { new JsonStringEnumConverter() },
  };

  private readonly ParkinApiFactory _factory;

  public ActiveSessionsByLotTests(ParkinApiFactory factory) => _factory = factory;

  private HttpClient CreateClient() => _factory.CreateClient();

  private static async Task LoginAsOperatorAsync(HttpClient client)
  {
    var response = await client.PostAsJsonAsync("/auth/login",
      new { Email = "operator@parkin.local", Password = "Operator!2345" });
    response.EnsureSuccessStatusCode();
  }

  private static async Task<Guid> CreateLotAsync(HttpClient client)
  {
    var response = await client.PostAsJsonAsync("/lots", new CreateLotRequest
    {
      Name = $"Sessions Lot {Guid.NewGuid():N}",
      Timezone = "Europe/Vilnius",
      AccessMode = AccessMode.Open,
    });
    response.EnsureSuccessStatusCode();
    var lot = await response.Content.ReadFromJsonAsync<LotRecord>(JsonOptions);
    lot.ShouldNotBeNull();

    var spaceResponse = await client.PostAsJsonAsync($"/lots/{lot.Id}/spaces",
      new { Label = "G1", Type = SpaceType.General.ToString() });
    spaceResponse.EnsureSuccessStatusCode();

    return lot.Id;
  }

  private static async Task<DriverRecord> CreateDriverWithPlateAsync(HttpClient client, string plate)
  {
    var driverResponse = await client.PostAsJsonAsync("/drivers", new { Name = $"Driver {Guid.NewGuid():N}" });
    driverResponse.EnsureSuccessStatusCode();
    var driver = await driverResponse.Content.ReadFromJsonAsync<DriverRecord>(JsonOptions);
    driver.ShouldNotBeNull();

    var plateResponse = await client.PostAsJsonAsync($"/drivers/{driver.Id}/plates", new { PlateNumber = plate });
    plateResponse.EnsureSuccessStatusCode();

    return driver;
  }

  private static async Task SendManualAsync(HttpClient client, Guid lotId, string plate, Direction direction)
  {
    var request = new HttpRequestMessage(HttpMethod.Post, $"/lots/{lotId}/manual-events")
    {
      Content = JsonContent.Create(new { Plate = plate, Direction = direction.ToString() }, options: JsonOptions),
    };
    request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
    var response = await client.SendAsync(request);
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
  }

  private static async Task<ActiveSessionListResponse> ListAsync(HttpClient client, Guid lotId)
  {
    var result = await client.GetFromJsonAsync<ActiveSessionListResponse>($"/lots/{lotId}/active-sessions", JsonOptions);
    result.ShouldNotBeNull();
    return result;
  }

  private static string NewPlate() => $"AS{Guid.NewGuid():N}"[..8].ToUpperInvariant();

  [Fact]
  public async Task Get_Unauthenticated_Returns401()
  {
    using var client = CreateClient();

    var response = await client.GetAsync($"/lots/{Guid.NewGuid()}/active-sessions");

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Get_KnownDriverEntry_ShowsDriverName()
  {
    using var client = CreateClient();
    await LoginAsOperatorAsync(client);
    var lotId = await CreateLotAsync(client);
    var plate = NewPlate();
    var driver = await CreateDriverWithPlateAsync(client, plate);
    await SendManualAsync(client, lotId, plate, Direction.Enter);

    var result = await ListAsync(client, lotId);

    var session = result.Items.ShouldHaveSingleItem();
    session.Plate.ShouldBe(plate);
    session.DriverId.ShouldBe(driver.Id);
    session.DriverName.ShouldBe(driver.Name);
  }

  [Fact]
  public async Task Get_UnknownPlate_ShowsNullDriver()
  {
    using var client = CreateClient();
    await LoginAsOperatorAsync(client);
    var lotId = await CreateLotAsync(client);
    var plate = NewPlate();
    await SendManualAsync(client, lotId, plate, Direction.Enter);

    var result = await ListAsync(client, lotId);

    var session = result.Items.ShouldHaveSingleItem();
    session.Plate.ShouldBe(plate);
    session.DriverId.ShouldBeNull();
    session.DriverName.ShouldBeNull();
  }

  [Fact]
  public async Task Get_ExitedSession_Disappears()
  {
    using var client = CreateClient();
    await LoginAsOperatorAsync(client);
    var lotId = await CreateLotAsync(client);
    var plate = NewPlate();
    await SendManualAsync(client, lotId, plate, Direction.Enter);
    (await ListAsync(client, lotId)).Items.Count.ShouldBe(1);

    await SendManualAsync(client, lotId, plate, Direction.Exit);

    (await ListAsync(client, lotId)).Items.ShouldBeEmpty();
  }

  [Fact]
  public async Task Get_ExcludesOtherLotsSessions()
  {
    using var client = CreateClient();
    await LoginAsOperatorAsync(client);
    var lotId = await CreateLotAsync(client);
    var otherLotId = await CreateLotAsync(client);
    var plate = NewPlate();
    await SendManualAsync(client, lotId, plate, Direction.Enter);
    await SendManualAsync(client, otherLotId, NewPlate(), Direction.Enter);

    var result = await ListAsync(client, lotId);

    result.Items.ShouldHaveSingleItem().Plate.ShouldBe(plate);
  }
}
