using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Parkin.Api.Features.AccessEvents;
using Parkin.Api.Features.ApiKeys;
using Parkin.Api.Features.ApiKeys.Create;
using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.Services;
using Parkin.Api.Features.Lots;
using Parkin.Api.Features.Lots.Create;
using Parkin.Api.Features.Occupancy;
using Shouldly;
using Xunit;

namespace Parkin.FunctionalTests.AccessEventFeatures;

public class AccessEventIngestionTests : IClassFixture<ParkinApiFactory>
{
  private const string Route = "/api/v1/access-events";

  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
  {
    Converters = { new JsonStringEnumConverter() },
  };

  private readonly ParkinApiFactory _factory;

  public AccessEventIngestionTests(ParkinApiFactory factory) => _factory = factory;

  private static async Task LoginAsAdminAsync(HttpClient client)
  {
    var response = await client.PostAsJsonAsync("/auth/login",
      new { Email = "admin@parkin.local", Password = "Admin!2345" });
    response.EnsureSuccessStatusCode();
  }

  private static async Task<string> CreateApiKeyAsync(HttpClient adminClient, Guid lotId)
  {
    var response = await adminClient.PostAsJsonAsync("/api-keys", new CreateApiKeyRequest
    {
      Name = $"Gate {Guid.NewGuid():N}",
      LotId = lotId,
    });
    response.EnsureSuccessStatusCode();
    var created = await response.Content.ReadFromJsonAsync<CreateApiKeyResponse>(JsonOptions);
    created.ShouldNotBeNull();
    return created.Key;
  }

  private static async Task<Guid> CreateLotAsync(HttpClient adminClient)
  {
    var response = await adminClient.PostAsJsonAsync("/lots", new CreateLotRequest
    {
      Name = $"Gate Lot {Guid.NewGuid():N}",
      Timezone = "Europe/Vilnius",
    });
    response.EnsureSuccessStatusCode();
    var lot = await response.Content.ReadFromJsonAsync<LotResponse>(JsonOptions);
    lot.ShouldNotBeNull();
    return lot.Id;
  }

  private static async Task CreateGeneralSpaceAsync(HttpClient adminClient, Guid lotId, string label)
  {
    var response = await adminClient.PostAsJsonAsync($"/lots/{lotId}/spaces",
      new { Label = label, Type = "General" });
    response.EnsureSuccessStatusCode();
  }

  private static HttpRequestMessage GateRequest(string apiKey, string? idempotencyKey, object body)
  {
    var request = new HttpRequestMessage(HttpMethod.Post, Route)
    {
      Content = JsonContent.Create(body, options: JsonOptions),
    };
    request.Headers.Add("X-Api-Key", apiKey);
    if (idempotencyKey is not null)
    {
      request.Headers.Add("Idempotency-Key", idempotencyKey);
    }

    return request;
  }

  private static object EnterBody(string plate) =>
    new { Plate = plate, Direction = "Enter", OccurredAt = DateTimeOffset.UtcNow };

  private static object ExitBody(string plate) =>
    new { Plate = plate, Direction = "Exit", OccurredAt = DateTimeOffset.UtcNow };

  [Fact]
  public async Task Post_WithoutApiKey_Returns401()
  {
    using var client = _factory.CreateClient();

    var response = await client.PostAsJsonAsync(Route, EnterBody("NOKEY 1"), JsonOptions);

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Post_WithStaffCookieButNoApiKey_Returns401()
  {
    using var client = _factory.CreateClient();
    await LoginAsAdminAsync(client);

    // The endpoint opts into the ApiKey scheme only.
    var response = await client.PostAsJsonAsync(Route, EnterBody("COOKIE 1"), JsonOptions);

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Post_WithRevokedApiKey_Returns401()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);

    var lotId = await CreateLotAsync(admin);
    var createResponse = await admin.PostAsJsonAsync("/api-keys", new CreateApiKeyRequest
    {
      Name = $"Revoked {Guid.NewGuid():N}",
      LotId = lotId,
    });
    createResponse.EnsureSuccessStatusCode();
    var created = await createResponse.Content.ReadFromJsonAsync<CreateApiKeyResponse>(JsonOptions);
    created.ShouldNotBeNull();

    var revokeResponse = await admin.PostAsync($"/api-keys/{created.Id}/revoke", content: null);
    revokeResponse.EnsureSuccessStatusCode();

    using var gate = _factory.CreateClient();
    var response = await gate.SendAsync(
      GateRequest(created.Key, "revoked-key-attempt", EnterBody("REVOKED 1")));

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Post_WithoutIdempotencyKey_Returns400()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var lotId = await CreateLotAsync(admin);
    var apiKey = await CreateApiKeyAsync(admin, lotId);

    using var gate = _factory.CreateClient();
    var response = await gate.SendAsync(GateRequest(apiKey, idempotencyKey: null, EnterBody("NOIDEM 1")));

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task Post_ToArchivedLot_Returns200WithLotArchivedDenial()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var lotId = await CreateLotAsync(admin);
    var apiKey = await CreateApiKeyAsync(admin, lotId);
    (await admin.PostAsync($"/lots/{lotId}/archive", content: null)).EnsureSuccessStatusCode();

    using var gate = _factory.CreateClient();
    var response = await gate.SendAsync(GateRequest(apiKey, $"archived-{Guid.NewGuid():N}", EnterBody("ARCHIVED 1")));

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    var decision = await response.Content.ReadFromJsonAsync<AccessEventDecisionResponse>(JsonOptions);
    decision.ShouldNotBeNull();
    decision.Decision.ShouldBe(Decision.Deny);
    decision.Reason.ShouldBe(DenyReason.LotArchived);
  }

  [Fact]
  public async Task Post_RecordsTheEventAgainstTheKeysLot_IgnoringAnyLotIdInTheBody()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var lotA = await CreateLotAsync(admin);
    var lotB = await CreateLotAsync(admin);
    await CreateGeneralSpaceAsync(admin, lotA, "A1");
    await CreateGeneralSpaceAsync(admin, lotB, "B1");
    var keyForA = await CreateApiKeyAsync(admin, lotA);

    using var gate = _factory.CreateClient();
    var response = await gate.SendAsync(GateRequest(keyForA, $"scoped-{Guid.NewGuid():N}",
      new { LotId = lotB, Plate = "SCOPED 1", Direction = "Enter", OccurredAt = DateTimeOffset.UtcNow }));

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    var decision = await response.Content.ReadFromJsonAsync<AccessEventDecisionResponse>(JsonOptions);
    decision.ShouldNotBeNull();
    decision.Decision.ShouldBe(Decision.Allow);

    var occupancyA = await admin.GetFromJsonAsync<LotOccupancyResponse>($"/lots/{lotA}/occupancy", JsonOptions);
    var occupancyB = await admin.GetFromJsonAsync<LotOccupancyResponse>($"/lots/{lotB}/occupancy", JsonOptions);
    occupancyA.ShouldNotBeNull();
    occupancyB.ShouldNotBeNull();
    occupancyA.GeneralUsed.ShouldBe(1);
    occupancyB.GeneralUsed.ShouldBe(0);
  }

  [Fact]
  public async Task CreateApiKey_WithoutLot_Returns400()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);

    var response = await admin.PostAsJsonAsync("/api-keys", new { Name = $"No lot {Guid.NewGuid():N}" });

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task CreateApiKey_ForUnknownLot_Returns404()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);

    var response = await admin.PostAsJsonAsync("/api-keys",
      new CreateApiKeyRequest { Name = $"Ghost {Guid.NewGuid():N}", LotId = Guid.NewGuid() });

    response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task CreateApiKey_ForArchivedLot_Returns400()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var lotId = await CreateLotAsync(admin);
    (await admin.PostAsync($"/lots/{lotId}/archive", content: null)).EnsureSuccessStatusCode();

    var response = await admin.PostAsJsonAsync("/api-keys",
      new CreateApiKeyRequest { Name = $"Archived {Guid.NewGuid():N}", LotId = lotId });

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task ListApiKeys_ExposesTheBoundLotIdAndName()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var lotId = await CreateLotAsync(admin);
    var createResponse = await admin.PostAsJsonAsync("/api-keys",
      new CreateApiKeyRequest { Name = $"Listed {Guid.NewGuid():N}", LotId = lotId });
    createResponse.EnsureSuccessStatusCode();
    var created = await createResponse.Content.ReadFromJsonAsync<CreateApiKeyResponse>(JsonOptions);
    created.ShouldNotBeNull();

    var keys = await admin.GetFromJsonAsync<List<ApiKeyResponse>>("/api-keys", JsonOptions);

    keys.ShouldNotBeNull();
    var listed = keys.Single(k => k.Id == created.Id);
    listed.LotId.ShouldBe(lotId);
    listed.LotName.ShouldBe(created.LotName);
  }
  [Fact]
  public async Task Post_EnterThenExit_OpensAndClosesASession()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var lotId = await CreateLotAsync(admin);
    var apiKey = await CreateApiKeyAsync(admin, lotId);
    await CreateGeneralSpaceAsync(admin, lotId, "G1");

    var plate = $"FN{Guid.NewGuid():N}"[..8];
    var enterKey = $"enter-{Guid.NewGuid():N}";

    using var gate = _factory.CreateClient();
    var enterResponse = await gate.SendAsync(GateRequest(apiKey, enterKey, EnterBody(plate)));

    enterResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    var entered = await enterResponse.Content.ReadFromJsonAsync<AccessEventDecisionResponse>(JsonOptions);
    entered.ShouldNotBeNull();
    entered.Decision.ShouldBe(Decision.Allow);
    entered.Pool.ShouldBe(SessionPool.General);
    entered.SessionId.ShouldNotBeNull();

    var replayResponse = await gate.SendAsync(GateRequest(apiKey, enterKey, EnterBody(plate)));
    replayResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    var replayed = await replayResponse.Content.ReadFromJsonAsync<AccessEventDecisionResponse>(JsonOptions);
    replayed.ShouldNotBeNull();
    replayed.EventId.ShouldBe(entered.EventId);
    replayed.SessionId.ShouldBe(entered.SessionId);

    var exitResponse = await gate.SendAsync(
      GateRequest(apiKey, $"exit-{Guid.NewGuid():N}", ExitBody(plate)));
    exitResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    var exited = await exitResponse.Content.ReadFromJsonAsync<AccessEventDecisionResponse>(JsonOptions);
    exited.ShouldNotBeNull();
    exited.Decision.ShouldBe(Decision.Allow);
    exited.SessionId.ShouldBe(entered.SessionId);
  }

  [Fact]
  public async Task Post_SameIdempotencyKeyFromTwoApiKeys_IsRecordedIndependently()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var lotId = await CreateLotAsync(admin);
    var northKey = await CreateApiKeyAsync(admin, lotId);
    var southKey = await CreateApiKeyAsync(admin, lotId);
    await CreateGeneralSpaceAsync(admin, lotId, "G1");
    await CreateGeneralSpaceAsync(admin, lotId, "G2");
    var sharedKey = $"1-{Guid.NewGuid():N}";

    using var gate = _factory.CreateClient();
    var northResponse = await gate.SendAsync(GateRequest(northKey, sharedKey, EnterBody("NORTH01")));
    var southResponse = await gate.SendAsync(GateRequest(southKey, sharedKey, EnterBody("SOUTH01")));

    northResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    southResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    var north = await northResponse.Content.ReadFromJsonAsync<AccessEventDecisionResponse>(JsonOptions);
    var south = await southResponse.Content.ReadFromJsonAsync<AccessEventDecisionResponse>(JsonOptions);
    north.ShouldNotBeNull();
    south.ShouldNotBeNull();
    north.Decision.ShouldBe(Decision.Allow);
    south.Decision.ShouldBe(Decision.Allow);
    south.EventId.ShouldNotBe(north.EventId);
    south.SessionId.ShouldNotBe(north.SessionId);
  }

  [Fact]
  public async Task Post_ReusedIdempotencyKeyForADifferentPlate_Returns409()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var lotId = await CreateLotAsync(admin);
    var apiKey = await CreateApiKeyAsync(admin, lotId);
    await CreateGeneralSpaceAsync(admin, lotId, "G1");
    var key = $"reused-{Guid.NewGuid():N}";

    using var gate = _factory.CreateClient();
    var first = await gate.SendAsync(GateRequest(apiKey, key, EnterBody("FIRST01")));
    var reused = await gate.SendAsync(GateRequest(apiKey, key, EnterBody("OTHER01")));

    first.StatusCode.ShouldBe(HttpStatusCode.OK);
    reused.StatusCode.ShouldBe(HttpStatusCode.Conflict);
  }

  [Fact]
  public async Task Post_ExitWithNoOpenSession_Returns200WithNoOpenSessionDenial()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var lotId = await CreateLotAsync(admin);
    var apiKey = await CreateApiKeyAsync(admin, lotId);

    using var gate = _factory.CreateClient();
    var response = await gate.SendAsync(GateRequest(apiKey, $"ghost-exit-{Guid.NewGuid():N}",
      ExitBody("GHOSTEXIT")));

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    var decision = await response.Content.ReadFromJsonAsync<AccessEventDecisionResponse>(JsonOptions);
    decision.ShouldNotBeNull();
    decision.Decision.ShouldBe(Decision.Deny);
    decision.Reason.ShouldBe(DenyReason.NoOpenSession);
    decision.SessionId.ShouldBeNull();
    decision.EventId.ShouldNotBeNull();
  }
}
