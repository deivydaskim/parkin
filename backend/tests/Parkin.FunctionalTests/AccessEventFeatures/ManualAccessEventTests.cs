using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Parkin.Api.AccessEventFeatures;
using Parkin.Api.ApiKeyFeatures.Create;
using Parkin.Api.AuditFeatures.List;
using Parkin.Api.AuthFeatures;
using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.Services;
using Parkin.Api.LotFeatures;
using Parkin.Api.LotFeatures.Create;
using Parkin.Api.OccupancyFeatures;
using Shouldly;
using Xunit;

namespace Parkin.FunctionalTests.AccessEventFeatures;

public class ManualAccessEventTests : IClassFixture<ParkinApiFactory>
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
  {
    Converters = { new JsonStringEnumConverter() },
  };

  private readonly ParkinApiFactory _factory;

  public ManualAccessEventTests(ParkinApiFactory factory) => _factory = factory;

  private static string ManualRoute(Guid lotId) => $"/lots/{lotId}/manual-events";

  private static async Task LoginAsync(HttpClient client, string email, string password)
  {
    var response = await client.PostAsJsonAsync("/auth/login", new { Email = email, Password = password });
    response.EnsureSuccessStatusCode();
  }

  private static Task LoginAsAdminAsync(HttpClient client) => LoginAsync(client, "admin@parkin.local", "Admin!2345");

  private static Task LoginAsOperatorAsync(HttpClient client) =>
    LoginAsync(client, "operator@parkin.local", "Operator!2345");

  private static async Task<Guid> CreateLotAsync(HttpClient client, AccessMode accessMode = AccessMode.Open)
  {
    var response = await client.PostAsJsonAsync("/lots", new CreateLotRequest
    {
      Name = $"Manual Lot {Guid.NewGuid():N}",
      Timezone = "Europe/Vilnius",
      AccessMode = accessMode,
    });
    response.EnsureSuccessStatusCode();
    var lot = await response.Content.ReadFromJsonAsync<LotRecord>(JsonOptions);
    lot.ShouldNotBeNull();

    var spaceResponse = await client.PostAsJsonAsync($"/lots/{lot.Id}/spaces",
      new { Label = "G1", Type = SpaceType.General.ToString() });
    spaceResponse.EnsureSuccessStatusCode();

    return lot.Id;
  }

  private static HttpRequestMessage ManualRequest(Guid lotId, string plate, Direction direction,
    string? idempotencyKey = null)
  {
    var request = new HttpRequestMessage(HttpMethod.Post, ManualRoute(lotId))
    {
      Content = JsonContent.Create(new { Plate = plate, Direction = direction.ToString() }, options: JsonOptions),
    };
    request.Headers.Add("Idempotency-Key", idempotencyKey ?? Guid.NewGuid().ToString());
    return request;
  }

  private static async Task<AccessEventDecisionRecord> SendManualAsync(HttpClient client, Guid lotId, string plate,
    Direction direction, string? idempotencyKey = null)
  {
    var response = await client.SendAsync(ManualRequest(lotId, plate, direction, idempotencyKey));
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    var decision = await response.Content.ReadFromJsonAsync<AccessEventDecisionRecord>(JsonOptions);
    decision.ShouldNotBeNull();
    return decision;
  }

  private static async Task<int> GeneralUsedAsync(HttpClient client, Guid lotId)
  {
    var occupancy = await client.GetFromJsonAsync<LotOccupancyRecord>($"/lots/{lotId}/occupancy", JsonOptions);
    occupancy.ShouldNotBeNull();
    return occupancy.GeneralUsed;
  }

  private static string NewPlate() => $"MN{Guid.NewGuid():N}"[..8].ToUpperInvariant();

  [Fact]
  public async Task Post_Unauthenticated_Returns401()
  {
    using var client = _factory.CreateClient();

    var response = await client.SendAsync(ManualRequest(Guid.NewGuid(), "AAA111", Direction.Enter));

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Post_WithApiKeyOnly_Returns401()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var lotId = await CreateLotAsync(admin);
    var keyResponse = await admin.PostAsJsonAsync("/api-keys", new CreateApiKeyRequest { Name = $"Gate {Guid.NewGuid():N}" });
    var apiKey = await keyResponse.Content.ReadFromJsonAsync<CreateApiKeyResponse>(JsonOptions);
    apiKey.ShouldNotBeNull();

    using var gate = _factory.CreateClient();
    var request = ManualRequest(lotId, NewPlate(), Direction.Enter);
    request.Headers.Add("X-Api-Key", apiKey.Key);
    var response = await gate.SendAsync(request);

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Post_UnknownLot_Returns404()
  {
    using var client = _factory.CreateClient();
    await LoginAsOperatorAsync(client);

    var response = await client.SendAsync(ManualRequest(Guid.NewGuid(), NewPlate(), Direction.Enter));

    response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task Post_MissingIdempotencyKey_Returns400()
  {
    using var client = _factory.CreateClient();
    await LoginAsOperatorAsync(client);
    var lotId = await CreateLotAsync(client);

    var request = new HttpRequestMessage(HttpMethod.Post, ManualRoute(lotId))
    {
      Content = JsonContent.Create(new { Plate = NewPlate(), Direction = "Enter" }, options: JsonOptions),
    };
    var response = await client.SendAsync(request);

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task Post_EnterThenExit_OpensAndClosesASessionThroughTheGatePath()
  {
    using var client = _factory.CreateClient();
    await LoginAsOperatorAsync(client);
    var lotId = await CreateLotAsync(client);
    var plate = NewPlate();

    var entry = await SendManualAsync(client, lotId, plate, Direction.Enter);

    entry.Decision.ShouldBe(Decision.Allow);
    entry.Pool.ShouldBe(SessionPool.General);
    entry.SessionId.ShouldNotBeNull();
    (await GeneralUsedAsync(client, lotId)).ShouldBe(1);

    var exit = await SendManualAsync(client, lotId, plate, Direction.Exit);

    exit.Decision.ShouldBe(Decision.Allow);
    exit.SessionId.ShouldBe(entry.SessionId);
    (await GeneralUsedAsync(client, lotId)).ShouldBe(0);
  }

  [Fact]
  public async Task Post_ReplayedIdempotencyKey_ReturnsOriginalWithoutDoubleCounting()
  {
    using var client = _factory.CreateClient();
    await LoginAsOperatorAsync(client);
    var lotId = await CreateLotAsync(client);
    var plate = NewPlate();
    var key = Guid.NewGuid().ToString();

    var first = await SendManualAsync(client, lotId, plate, Direction.Enter, key);
    var replay = await SendManualAsync(client, lotId, plate, Direction.Enter, key);

    replay.EventId.ShouldBe(first.EventId);
    replay.SessionId.ShouldBe(first.SessionId);
    (await GeneralUsedAsync(client, lotId)).ShouldBe(1);
  }

  [Fact]
  public async Task Post_UnknownPlateOnRestrictedLot_DeniesWith200()
  {
    using var client = _factory.CreateClient();
    await LoginAsOperatorAsync(client);
    var lotId = await CreateLotAsync(client, AccessMode.Restricted);

    var decision = await SendManualAsync(client, lotId, NewPlate(), Direction.Enter);

    decision.Decision.ShouldBe(Decision.Deny);
    decision.Reason.ShouldBe(DenyReason.NotAuthorized);
    (await GeneralUsedAsync(client, lotId)).ShouldBe(0);
  }

  [Fact]
  public async Task Post_ExitWithNoOpenSession_DeniesWithoutGoingNegative()
  {
    using var client = _factory.CreateClient();
    await LoginAsOperatorAsync(client);
    var lotId = await CreateLotAsync(client);

    var decision = await SendManualAsync(client, lotId, NewPlate(), Direction.Exit);

    decision.Decision.ShouldBe(Decision.Deny);
    decision.Reason.ShouldBe(DenyReason.NoOpenSession);
    (await GeneralUsedAsync(client, lotId)).ShouldBe(0);
  }

  [Fact]
  public async Task Post_ManualEvent_IsAuditedAsTheActingStaffMemberWithSourceManual()
  {
    using var operatorClient = _factory.CreateClient();
    await LoginAsOperatorAsync(operatorClient);
    var me = await operatorClient.GetFromJsonAsync<CurrentUserResponse>("/auth/me", JsonOptions);
    me.ShouldNotBeNull();
    var lotId = await CreateLotAsync(operatorClient);

    var decision = await SendManualAsync(operatorClient, lotId, NewPlate(), Direction.Enter);
    decision.EventId.ShouldNotBeNull();

    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var page = await admin.GetFromJsonAsync<AuditListResponse>(
      $"/audit?entity=AccessEvent&actor_type=Staff&actor={me.Id}&per_page=100", JsonOptions);
    page.ShouldNotBeNull();

    var entry = page.Items.Single(e => e.EntityId == decision.EventId);
    entry.ActorType.ShouldBe("Staff");
    entry.ActorId.ShouldBe(me.Id);
    entry.Action.ShouldBe("access_event.ingested");
    entry.MetadataJson.ShouldNotBeNull();
    entry.MetadataJson.ShouldContain("Manual");
  }
}
