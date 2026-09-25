using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Parkin.Api.AccessEventFeatures;
using Parkin.Api.AccessEventFeatures.List;
using Parkin.Api.AuditFeatures.List;
using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.Services;
using Parkin.Api.DriverFeatures;
using Parkin.Api.DriverFeatures.List;
using Parkin.Api.GrantFeatures.List;
using Parkin.Api.LotFeatures;
using Parkin.Api.LotFeatures.List;
using Parkin.Api.OccupancyFeatures;
using Parkin.Api.SpaceFeatures;
using Parkin.Api.SpaceFeatures.List;
using Shouldly;
using Xunit;

namespace Parkin.FunctionalTests.ReadModelFeatures;

public class OperatorConsoleReadModelTests : IClassFixture<ParkinApiFactory>
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
  {
    Converters = { new JsonStringEnumConverter() },
  };

  private readonly ParkinApiFactory _factory;

  public OperatorConsoleReadModelTests(ParkinApiFactory factory) => _factory = factory;

  private static async Task LoginAsync(HttpClient client, string email, string password)
  {
    var response = await client.PostAsJsonAsync("/auth/login", new { Email = email, Password = password });
    response.EnsureSuccessStatusCode();
  }

  private static Task LoginAsAdminAsync(HttpClient client) =>
    LoginAsync(client, "admin@parkin.local", "Admin!2345");

  private static Task LoginAsOperatorAsync(HttpClient client) =>
    LoginAsync(client, "operator@parkin.local", "Operator!2345");

  private static string Unique() => Guid.NewGuid().ToString("N")[..10];

  private static async Task<T> ReadAsync<T>(HttpResponseMessage response) where T : class
  {
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    var body = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    body.ShouldNotBeNull();
    return body;
  }

  private static async Task<LotRecord> CreateLotAsync(HttpClient client, string name, string? address = null)
  {
    var response = await client.PostAsJsonAsync("/lots",
      new { Name = name, Address = address, Timezone = "Europe/Vilnius", AccessMode = "Open", FullBehavior = "Block" });
    response.EnsureSuccessStatusCode();
    var lot = await response.Content.ReadFromJsonAsync<LotRecord>(JsonOptions);
    lot.ShouldNotBeNull();
    return lot;
  }

  private static async Task<SpaceRecord> CreateSpaceAsync(HttpClient client, Guid lotId, string label, SpaceType type)
  {
    var response = await client.PostAsJsonAsync($"/lots/{lotId}/spaces", new { Label = label, Type = type.ToString() });
    response.EnsureSuccessStatusCode();
    var space = await response.Content.ReadFromJsonAsync<SpaceRecord>(JsonOptions);
    space.ShouldNotBeNull();
    return space;
  }

  private static async Task<DriverRecord> CreateDriverAsync(HttpClient client, string name, string? plate = null)
  {
    var response = await client.PostAsJsonAsync("/drivers", new { Name = name });
    response.EnsureSuccessStatusCode();
    var driver = await response.Content.ReadFromJsonAsync<DriverRecord>(JsonOptions);
    driver.ShouldNotBeNull();

    if (plate is not null)
    {
      var plateResponse = await client.PostAsJsonAsync($"/drivers/{driver.Id}/plates", new { PlateNumber = plate });
      plateResponse.EnsureSuccessStatusCode();
    }

    return driver;
  }

  private static async Task<AccessEventDecisionRecord> RecordManualAsync(HttpClient client, Guid lotId, string plate,
    Direction direction)
  {
    var request = new HttpRequestMessage(HttpMethod.Post, $"/lots/{lotId}/manual-events")
    {
      Content = JsonContent.Create(new { Plate = plate, Direction = direction.ToString() }),
    };
    request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
    return await ReadAsync<AccessEventDecisionRecord>(await client.SendAsync(request));
  }

  [Fact]
  public async Task ListLots_WithSearch_MatchesNameOrAddressCaseInsensitively()
  {
    using var client = _factory.CreateClient();
    await LoginAsAdminAsync(client);
    var token = Unique();
    var byName = await CreateLotAsync(client, $"Harbour {token} Garage");
    var byAddress = await CreateLotAsync(client, $"Unrelated {Unique()}", $"12 {token.ToUpperInvariant()} Street");
    await CreateLotAsync(client, $"Other {Unique()}");

    var result = await ReadAsync<LotListResponse>(await client.GetAsync($"/lots?search={token}"));

    result.Items.Select(l => l.Id).ShouldBe([byName.Id, byAddress.Id], ignoreOrder: true);
    result.TotalCount.ShouldBe(2);
  }

  [Fact]
  public async Task ListDrivers_WithSearch_MatchesNameContactOrPlate()
  {
    using var client = _factory.CreateClient();
    await LoginAsAdminAsync(client);
    var token = Unique();
    var plate = $"S{Unique()}"[..8].ToUpperInvariant();
    var byName = await CreateDriverAsync(client, $"Rasa {token}");
    var byPlate = await CreateDriverAsync(client, $"Jonas {Unique()}", plate);

    var nameResult = await ReadAsync<DriverListResponse>(await client.GetAsync($"/drivers?search={token}"));
    nameResult.Items.Select(d => d.Id).ShouldBe([byName.Id]);

    var plateSearch = Uri.EscapeDataString(plate[..3] + " " + plate[3..].ToLowerInvariant());
    var plateResult = await ReadAsync<DriverListResponse>(await client.GetAsync($"/drivers?search={plateSearch}"));
    plateResult.Items.Select(d => d.Id).ShouldBe([byPlate.Id]);
  }

  [Fact]
  public async Task ListSpaces_ProjectsReservationHolder_AndFiltersByTypeAndLabel()
  {
    using var client = _factory.CreateClient();
    await LoginAsAdminAsync(client);
    var lot = await CreateLotAsync(client, $"Spaces {Unique()}");
    await CreateSpaceAsync(client, lot.Id, "G-01", SpaceType.General);
    var reserved = await CreateSpaceAsync(client, lot.Id, "R-01", SpaceType.Reserved);
    await CreateSpaceAsync(client, lot.Id, "R-02", SpaceType.Reserved);
    var driver = await CreateDriverAsync(client, $"Holder {Unique()}");
    var reserveResponse = await client.PostAsJsonAsync("/reservations",
      new { SpaceId = reserved.Id, DriverId = driver.Id });
    reserveResponse.EnsureSuccessStatusCode();

    var all = await ReadAsync<SpaceListResponse>(await client.GetAsync($"/lots/{lot.Id}/spaces"));
    var holderRow = all.Items.Single(s => s.Id == reserved.Id);
    holderRow.ReservedDriverId.ShouldBe(driver.Id);
    holderRow.ReservedDriverName.ShouldBe(driver.Name);
    all.Items.Where(s => s.Id != reserved.Id).ShouldAllBe(s => s.ReservedDriverId == null);

    var reservedOnly = await ReadAsync<SpaceListResponse>(
      await client.GetAsync($"/lots/{lot.Id}/spaces?type=Reserved"));
    reservedOnly.Items.Select(s => s.Label).ShouldBe(["R-01", "R-02"]);

    var byLabel = await ReadAsync<SpaceListResponse>(await client.GetAsync($"/lots/{lot.Id}/spaces?search=r-02"));
    byLabel.Items.Select(s => s.Label).ShouldBe(["R-02"]);
  }

  [Fact]
  public async Task ListGrants_IncludesLotName()
  {
    using var client = _factory.CreateClient();
    await LoginAsAdminAsync(client);
    var lot = await CreateLotAsync(client, $"Granted {Unique()}");
    var driver = await CreateDriverAsync(client, $"Grantee {Unique()}");
    var grantResponse = await client.PostAsJsonAsync("/grants", new { DriverId = driver.Id, LotId = lot.Id });
    grantResponse.EnsureSuccessStatusCode();

    var grants = await ReadAsync<GrantListResponse>(await client.GetAsync($"/drivers/{driver.Id}/grants"));

    grants.Items.ShouldHaveSingleItem().ParkingLotName.ShouldBe(lot.Name);
  }

  [Fact]
  public async Task ListAudit_IncludesStaffActorName()
  {
    using var client = _factory.CreateClient();
    await LoginAsAdminAsync(client);
    var me = await client.GetFromJsonAsync<JsonElement>("/auth/me");
    var adminId = me.GetProperty("id").GetString();
    await CreateLotAsync(client, $"Audited {Unique()}");

    var audit = await ReadAsync<AuditListResponse>(
      await client.GetAsync($"/audit?entity=ParkingLot&actor={adminId}&per_page=5"));

    audit.Items.ShouldNotBeEmpty();
    audit.Items.ShouldAllBe(e => e.ActorName == "System Administrator");
  }

  [Fact]
  public async Task ListOccupancy_ReturnsEveryActiveLotWithNameAndCounts()
  {
    using var client = _factory.CreateClient();
    await LoginAsOperatorAsync(client);
    var lot = await CreateLotAsync(client, $"Dashboard {Unique()}");
    await CreateSpaceAsync(client, lot.Id, "G1", SpaceType.General);
    await CreateSpaceAsync(client, lot.Id, "G2", SpaceType.General);
    await CreateSpaceAsync(client, lot.Id, "R1", SpaceType.Reserved);
    await RecordManualAsync(client, lot.Id, $"D{Unique()}"[..8], Direction.Enter);
    var archived = await CreateLotAsync(client, $"Archived {Unique()}");
    (await client.PostAsync($"/lots/{archived.Id}/archive", null)).EnsureSuccessStatusCode();

    var occupancies = await ReadAsync<List<LotOccupancyRecord>>(await client.GetAsync("/occupancy"));

    var row = occupancies.Single(o => o.LotId == lot.Id);
    row.LotName.ShouldBe(lot.Name);
    row.GeneralCapacity.ShouldBe(2);
    row.GeneralUsed.ShouldBe(1);
    row.GeneralFree.ShouldBe(1);
    row.ReservedSpaceCount.ShouldBe(1);
    occupancies.ShouldNotContain(o => o.LotId == archived.Id);
  }

  [Fact]
  public async Task ListOccupancy_Unauthenticated_Returns401()
  {
    using var client = _factory.CreateClient();

    var response = await client.GetAsync("/occupancy");

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task ListAccessEvents_ReturnsNewestFirstWithDecisionDetails()
  {
    using var client = _factory.CreateClient();
    await LoginAsOperatorAsync(client);
    var lot = await CreateLotAsync(client, $"Feed {Unique()}");
    await CreateSpaceAsync(client, lot.Id, "G1", SpaceType.General);
    var plate = $"F{Unique()}"[..8].ToUpperInvariant();
    var driver = await CreateDriverAsync(client, $"Feed Driver {Unique()}", plate);

    await RecordManualAsync(client, lot.Id, plate, Direction.Enter);
    await RecordManualAsync(client, lot.Id, $"U{Unique()}"[..8].ToUpperInvariant(), Direction.Exit);

    var feed = await ReadAsync<AccessEventListResponse>(await client.GetAsync($"/lots/{lot.Id}/access-events"));

    feed.TotalCount.ShouldBe(2);
    var (latest, earliest) = (feed.Items[0], feed.Items[1]);

    latest.Direction.ShouldBe(Direction.Exit);
    latest.Decision.ShouldBe(Decision.Deny);
    latest.Reason.ShouldBe(DenyReason.NoOpenSession);
    latest.Source.ShouldBe(EventSource.Manual);

    earliest.Plate.ShouldBe(plate);
    earliest.Decision.ShouldBe(Decision.Allow);
    earliest.Pool.ShouldBe(SessionPool.General);
    earliest.DriverId.ShouldBe(driver.Id);
    earliest.DriverName.ShouldBe(driver.Name);
  }

  [Fact]
  public async Task ListAccessEvents_UnknownLot_Returns404()
  {
    using var client = _factory.CreateClient();
    await LoginAsOperatorAsync(client);

    var response = await client.GetAsync($"/lots/{Guid.NewGuid()}/access-events");

    response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task ListAccessEvents_Unauthenticated_Returns401()
  {
    using var client = _factory.CreateClient();

    var response = await client.GetAsync($"/lots/{Guid.NewGuid()}/access-events");

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task ListAccessEvents_WithGateApiKeyInsteadOfStaffSession_IsRejected()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var lot = await CreateLotAsync(admin, $"Keyed {Unique()}");
    var keyResponse = await admin.PostAsJsonAsync("/api-keys", new { Name = $"Gate {Unique()}" });
    keyResponse.EnsureSuccessStatusCode();
    var key = (await keyResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("key").GetString();

    using var gate = _factory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Get, $"/lots/{lot.Id}/access-events");
    request.Headers.Add("X-Api-Key", key);
    var response = await gate.SendAsync(request);

    response.StatusCode.ShouldBeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
  }
}
