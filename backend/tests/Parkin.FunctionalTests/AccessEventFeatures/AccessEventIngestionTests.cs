using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Parkin.Api.AccessEventFeatures;
using Parkin.Api.ApiKeyFeatures.Create;
using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.Services;
using Parkin.Api.LotFeatures;
using Parkin.Api.LotFeatures.Create;
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

  private static async Task<string> CreateApiKeyAsync(HttpClient adminClient)
  {
    var response = await adminClient.PostAsJsonAsync("/api-keys", new CreateApiKeyRequest
    {
      Name = $"Gate {Guid.NewGuid():N}",
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
    var lot = await response.Content.ReadFromJsonAsync<LotRecord>(JsonOptions);
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

  private static object EnterBody(Guid lotId, string plate) =>
    new { LotId = lotId, Plate = plate, Direction = "Enter", OccurredAt = DateTimeOffset.UtcNow };

  private static object ExitBody(Guid lotId, string plate) =>
    new { LotId = lotId, Plate = plate, Direction = "Exit", OccurredAt = DateTimeOffset.UtcNow };

  [Fact]
  public async Task Post_WithoutApiKey_Returns401()
  {
    using var client = _factory.CreateClient();

    var response = await client.PostAsJsonAsync(Route, EnterBody(Guid.NewGuid(), "NOKEY 1"), JsonOptions);

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Post_WithStaffCookieButNoApiKey_Returns401()
  {
    using var client = _factory.CreateClient();
    await LoginAsAdminAsync(client);

    // The endpoint opts into the ApiKey scheme only.
    var response = await client.PostAsJsonAsync(Route, EnterBody(Guid.NewGuid(), "COOKIE 1"), JsonOptions);

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Post_WithRevokedApiKey_Returns401()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);

    var createResponse = await admin.PostAsJsonAsync("/api-keys", new CreateApiKeyRequest
    {
      Name = $"Revoked {Guid.NewGuid():N}",
    });
    createResponse.EnsureSuccessStatusCode();
    var created = await createResponse.Content.ReadFromJsonAsync<CreateApiKeyResponse>(JsonOptions);
    created.ShouldNotBeNull();

    var revokeResponse = await admin.PostAsync($"/api-keys/{created.Id}/revoke", content: null);
    revokeResponse.EnsureSuccessStatusCode();

    using var gate = _factory.CreateClient();
    var response = await gate.SendAsync(
      GateRequest(created.Key, "revoked-key-attempt", EnterBody(Guid.NewGuid(), "REVOKED 1")));

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Post_WithoutIdempotencyKey_Returns400()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var apiKey = await CreateApiKeyAsync(admin);
    var lotId = await CreateLotAsync(admin);

    using var gate = _factory.CreateClient();
    var response = await gate.SendAsync(GateRequest(apiKey, idempotencyKey: null, EnterBody(lotId, "NOIDEM 1")));

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task Post_UnknownLot_Returns200WithLotNotFoundDenial()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var apiKey = await CreateApiKeyAsync(admin);

    using var gate = _factory.CreateClient();
    var response = await gate.SendAsync(GateRequest(apiKey, $"unknown-lot-{Guid.NewGuid():N}",
      EnterBody(Guid.NewGuid(), "GHOSTLOT 1")));

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    var decision = await response.Content.ReadFromJsonAsync<AccessEventDecisionRecord>(JsonOptions);
    decision.ShouldNotBeNull();
    decision.Decision.ShouldBe(Decision.Deny);
    decision.Reason.ShouldBe(DenyReason.LotNotFound);
    decision.EventId.ShouldBeNull();
  }

  [Fact]
  public async Task Post_EnterThenExit_OpensAndClosesASession()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var apiKey = await CreateApiKeyAsync(admin);
    var lotId = await CreateLotAsync(admin);
    await CreateGeneralSpaceAsync(admin, lotId, "G1");

    var plate = $"FN{Guid.NewGuid():N}"[..8];
    var enterKey = $"enter-{Guid.NewGuid():N}";

    using var gate = _factory.CreateClient();
    var enterResponse = await gate.SendAsync(GateRequest(apiKey, enterKey, EnterBody(lotId, plate)));

    enterResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    var entered = await enterResponse.Content.ReadFromJsonAsync<AccessEventDecisionRecord>(JsonOptions);
    entered.ShouldNotBeNull();
    entered.Decision.ShouldBe(Decision.Allow);
    entered.Pool.ShouldBe(SessionPool.General);
    entered.SessionId.ShouldNotBeNull();

    var replayResponse = await gate.SendAsync(GateRequest(apiKey, enterKey, EnterBody(lotId, plate)));
    replayResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    var replayed = await replayResponse.Content.ReadFromJsonAsync<AccessEventDecisionRecord>(JsonOptions);
    replayed.ShouldNotBeNull();
    replayed.EventId.ShouldBe(entered.EventId);
    replayed.SessionId.ShouldBe(entered.SessionId);

    var exitResponse = await gate.SendAsync(
      GateRequest(apiKey, $"exit-{Guid.NewGuid():N}", ExitBody(lotId, plate)));
    exitResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    var exited = await exitResponse.Content.ReadFromJsonAsync<AccessEventDecisionRecord>(JsonOptions);
    exited.ShouldNotBeNull();
    exited.Decision.ShouldBe(Decision.Allow);
    exited.SessionId.ShouldBe(entered.SessionId);
  }

  [Fact]
  public async Task Post_ExitWithNoOpenSession_Returns200WithNoOpenSessionDenial()
  {
    using var admin = _factory.CreateClient();
    await LoginAsAdminAsync(admin);
    var apiKey = await CreateApiKeyAsync(admin);
    var lotId = await CreateLotAsync(admin);

    using var gate = _factory.CreateClient();
    var response = await gate.SendAsync(GateRequest(apiKey, $"ghost-exit-{Guid.NewGuid():N}",
      ExitBody(lotId, "GHOSTEXIT")));

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    var decision = await response.Content.ReadFromJsonAsync<AccessEventDecisionRecord>(JsonOptions);
    decision.ShouldNotBeNull();
    decision.Decision.ShouldBe(Decision.Deny);
    decision.Reason.ShouldBe(DenyReason.NoOpenSession);
    decision.SessionId.ShouldBeNull();
    decision.EventId.ShouldNotBeNull();
  }
}
