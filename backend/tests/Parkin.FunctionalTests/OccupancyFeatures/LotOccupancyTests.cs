using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Parkin.Api.ApiKeyFeatures.Create;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.DriverFeatures;
using Parkin.Api.LotFeatures;
using Parkin.Api.LotFeatures.Create;
using Parkin.Api.OccupancyFeatures;
using Parkin.Api.SpaceFeatures;
using Shouldly;
using Xunit;

namespace Parkin.FunctionalTests.OccupancyFeatures;

// Every number here is produced by driving the real gate endpoint, not by seeding session rows -
// the point is that the read model and the ingestion path agree.
public class LotOccupancyTests : IClassFixture<ParkinApiFactory>
{
  private const string GateRoute = "/api/v1/access-events";

  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
  {
    Converters = { new JsonStringEnumConverter() },
  };

  private readonly ParkinApiFactory _factory;

  public LotOccupancyTests(ParkinApiFactory factory) => _factory = factory;

  private static string OccupancyRoute(Guid lotId) => $"/lots/{lotId}/occupancy";

  private static async Task LoginAsAdminAsync(HttpClient client)
  {
    var response = await client.PostAsJsonAsync("/auth/login",
      new { Email = "admin@parkin.local", Password = "Admin!2345" });
    response.EnsureSuccessStatusCode();
  }

  private static async Task<string> CreateApiKeyAsync(HttpClient adminClient)
  {
    var response = await adminClient.PostAsJsonAsync("/api-keys", new CreateApiKeyRequest
    {
      Name = $"Occupancy gate {Guid.NewGuid():N}",
    });
    response.EnsureSuccessStatusCode();
    var created = await response.Content.ReadFromJsonAsync<CreateApiKeyResponse>(JsonOptions);
    created.ShouldNotBeNull();
    return created.Key;
  }

  private static async Task<Guid> CreateLotAsync(HttpClient adminClient,
    FullBehavior fullBehavior = FullBehavior.Block, AccessMode accessMode = AccessMode.Open)
  {
    var response = await adminClient.PostAsJsonAsync("/lots", new CreateLotRequest
    {
      Name = $"Occupancy Lot {Guid.NewGuid():N}",
      Timezone = "Europe/Vilnius",
      AccessMode = accessMode,
      FullBehavior = fullBehavior,
    });
    response.EnsureSuccessStatusCode();
    var lot = await response.Content.ReadFromJsonAsync<LotRecord>(JsonOptions);
    lot.ShouldNotBeNull();
    return lot.Id;
  }

  private static async Task<Guid> CreateSpaceAsync(HttpClient adminClient, Guid lotId, string label, SpaceType type)
  {
    var response = await adminClient.PostAsJsonAsync($"/lots/{lotId}/spaces",
      new { Label = label, Type = type.ToString() });
    response.EnsureSuccessStatusCode();
    var space = await response.Content.ReadFromJsonAsync<SpaceRecord>(JsonOptions);
    space.ShouldNotBeNull();
    return space.Id;
  }

  private static async Task<Guid> CreateDriverWithPlateAsync(HttpClient adminClient, string plate)
  {
    var driverResponse = await adminClient.PostAsJsonAsync("/drivers",
      new { Name = $"Driver {Guid.NewGuid():N}" });
    driverResponse.EnsureSuccessStatusCode();
    var driver = await driverResponse.Content.ReadFromJsonAsync<DriverRecord>(JsonOptions);
    driver.ShouldNotBeNull();

    var plateResponse = await adminClient.PostAsJsonAsync($"/drivers/{driver.Id}/plates",
      new { PlateNumber = plate });
    plateResponse.EnsureSuccessStatusCode();

    return driver.Id;
  }

  private static async Task ReserveAsync(HttpClient adminClient, Guid spaceId, Guid driverId)
  {
    var response = await adminClient.PostAsJsonAsync("/reservations",
      new { SpaceId = spaceId, DriverId = driverId });
    response.EnsureSuccessStatusCode();
  }

  private static HttpRequestMessage GateRequest(string apiKey, string idempotencyKey, object body)
  {
    var request = new HttpRequestMessage(HttpMethod.Post, GateRoute)
    {
      Content = JsonContent.Create(body, options: JsonOptions),
    };
    request.Headers.Add("X-Api-Key", apiKey);
    request.Headers.Add("Idempotency-Key", idempotencyKey);
    return request;
  }

  private static object EnterBody(Guid lotId, string plate) =>
    new { LotId = lotId, Plate = plate, Direction = "Enter", OccurredAt = DateTimeOffset.UtcNow };

  private static object ExitBody(Guid lotId, string plate) =>
    new { LotId = lotId, Plate = plate, Direction = "Exit", OccurredAt = DateTimeOffset.UtcNow };

  private async Task GateAsync(string apiKey, string idempotencyKey, object body)
  {
    using var gate = _factory.CreateClient();
    var response = await gate.SendAsync(GateRequest(apiKey, idempotencyKey, body));
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
  }

  private static async Task<LotOccupancyRecord> GetOccupancyAsync(HttpClient client, Guid lotId)
  {
    var response = await client.GetAsync(OccupancyRoute(lotId));
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    var occupancy = await response.Content.ReadFromJsonAsync<LotOccupancyRecord>(JsonOptions);
    occupancy.ShouldNotBeNull();
    return occupancy;
  }

  private static string NewPlate() => $"OC{Guid.NewGuid():N}"[..8].ToUpperInvariant();

  [Fact]
  public async Task Get_Unauthenticated_Returns401()
  {
    using var client = _factory.CreateClient();

    var response = await client.GetAsync(OccupancyRoute(Guid.NewGuid()));

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Get_UnknownLot_Returns404()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);

    var response = await admin.GetAsync(OccupancyRoute(Guid.NewGuid()));

    response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task Get_EmptyLot_ReportsCapacityAllFree()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var lotId = await CreateLotAsync(admin);
    await CreateSpaceAsync(admin, lotId, "G1", SpaceType.General);
    await CreateSpaceAsync(admin, lotId, "G2", SpaceType.General);
    await CreateSpaceAsync(admin, lotId, "R1", SpaceType.Reserved);

    var occupancy = await GetOccupancyAsync(admin, lotId);

    occupancy.LotId.ShouldBe(lotId);
    occupancy.GeneralCapacity.ShouldBe(2);
    occupancy.GeneralUsed.ShouldBe(0);
    occupancy.GeneralFree.ShouldBe(2);
    occupancy.IsGeneralPoolFull.ShouldBeFalse();
    occupancy.IsOverCapacity.ShouldBeFalse();
    occupancy.ReservedSpaceCount.ShouldBe(1);
    occupancy.ReservedOccupied.ShouldBe(0);
  }

  [Fact]
  public async Task Get_AfterGateEnterThenExit_TracksOccupancyBothWays()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var apiKey = await CreateApiKeyAsync(admin);
    var lotId = await CreateLotAsync(admin);
    await CreateSpaceAsync(admin, lotId, "G1", SpaceType.General);
    await CreateSpaceAsync(admin, lotId, "G2", SpaceType.General);

    var plate = NewPlate();

    await GateAsync(apiKey, $"enter-{Guid.NewGuid():N}", EnterBody(lotId, plate));

    var afterEnter = await GetOccupancyAsync(admin, lotId);
    afterEnter.GeneralUsed.ShouldBe(1);
    afterEnter.GeneralFree.ShouldBe(1);

    await GateAsync(apiKey, $"exit-{Guid.NewGuid():N}", ExitBody(lotId, plate));

    var afterExit = await GetOccupancyAsync(admin, lotId);
    afterExit.GeneralUsed.ShouldBe(0);
    afterExit.GeneralFree.ShouldBe(2);
  }

  [Fact]
  public async Task Get_AfterReplayedIdempotencyKey_DoesNotDoubleCount()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var apiKey = await CreateApiKeyAsync(admin);
    var lotId = await CreateLotAsync(admin);
    await CreateSpaceAsync(admin, lotId, "G1", SpaceType.General);
    await CreateSpaceAsync(admin, lotId, "G2", SpaceType.General);

    var plate = NewPlate();
    var idempotencyKey = $"replay-{Guid.NewGuid():N}";

    await GateAsync(apiKey, idempotencyKey, EnterBody(lotId, plate));
    await GateAsync(apiKey, idempotencyKey, EnterBody(lotId, plate));

    var occupancy = await GetOccupancyAsync(admin, lotId);
    occupancy.GeneralUsed.ShouldBe(1);
    occupancy.GeneralFree.ShouldBe(1);
  }

  [Fact]
  public async Task Get_ExactlyFullLot_FlagsFullWithoutOverCapacity()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var apiKey = await CreateApiKeyAsync(admin);
    var lotId = await CreateLotAsync(admin);
    await CreateSpaceAsync(admin, lotId, "G1", SpaceType.General);

    await GateAsync(apiKey, $"full-{Guid.NewGuid():N}", EnterBody(lotId, NewPlate()));

    var occupancy = await GetOccupancyAsync(admin, lotId);
    occupancy.GeneralUsed.ShouldBe(1);
    occupancy.GeneralFree.ShouldBe(0);
    occupancy.IsGeneralPoolFull.ShouldBeTrue();
    occupancy.IsOverCapacity.ShouldBeFalse();
  }

  [Fact]
  public async Task Get_OverflowLotPastCapacity_FloorsFreeAtZeroAndFlagsOverCapacity()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var apiKey = await CreateApiKeyAsync(admin);
    var lotId = await CreateLotAsync(admin, FullBehavior.AllowOverflow);
    await CreateSpaceAsync(admin, lotId, "G1", SpaceType.General);

    await GateAsync(apiKey, $"overflow-a-{Guid.NewGuid():N}", EnterBody(lotId, NewPlate()));
    await GateAsync(apiKey, $"overflow-b-{Guid.NewGuid():N}", EnterBody(lotId, NewPlate()));

    var occupancy = await GetOccupancyAsync(admin, lotId);
    occupancy.GeneralCapacity.ShouldBe(1);
    occupancy.GeneralUsed.ShouldBe(2);
    occupancy.GeneralFree.ShouldBe(0);
    occupancy.IsOverCapacity.ShouldBeTrue();
  }

  [Fact]
  public async Task Get_ReservedHolderEnters_CountsAgainstReservedNotGeneral()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var apiKey = await CreateApiKeyAsync(admin);
    var lotId = await CreateLotAsync(admin);
    await CreateSpaceAsync(admin, lotId, "G1", SpaceType.General);
    var reservedSpaceId = await CreateSpaceAsync(admin, lotId, "R1", SpaceType.Reserved);

    var plate = NewPlate();
    var driverId = await CreateDriverWithPlateAsync(admin, plate);
    await ReserveAsync(admin, reservedSpaceId, driverId);

    await GateAsync(apiKey, $"reserved-{Guid.NewGuid():N}", EnterBody(lotId, plate));

    var occupancy = await GetOccupancyAsync(admin, lotId);
    occupancy.ReservedOccupied.ShouldBe(1);
    occupancy.ReservedSpaceCount.ShouldBe(1);
    occupancy.GeneralUsed.ShouldBe(0);
    occupancy.GeneralFree.ShouldBe(1);
  }

  [Fact]
  public async Task Get_DeactivatedGeneralSpace_LeavesCapacity()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var lotId = await CreateLotAsync(admin);
    await CreateSpaceAsync(admin, lotId, "G1", SpaceType.General);
    var spaceId = await CreateSpaceAsync(admin, lotId, "G2", SpaceType.General);

    var deactivate = await admin.PostAsync($"/spaces/{spaceId}/deactivate", content: null);
    deactivate.EnsureSuccessStatusCode();

    var occupancy = await GetOccupancyAsync(admin, lotId);
    occupancy.GeneralCapacity.ShouldBe(1);
    occupancy.GeneralFree.ShouldBe(1);
  }
}
