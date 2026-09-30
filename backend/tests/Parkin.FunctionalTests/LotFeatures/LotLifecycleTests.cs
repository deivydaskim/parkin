using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Features.Lots;
using Shouldly;
using Xunit;

namespace Parkin.FunctionalTests.LotFeatures;

public class LotLifecycleTests : IClassFixture<ParkinApiFactory>
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
  {
    Converters = { new JsonStringEnumConverter() },
  };

  private readonly ParkinApiFactory _factory;

  public LotLifecycleTests(ParkinApiFactory factory) => _factory = factory;

  private async Task<HttpClient> AdminClientAsync()
  {
    var client = _factory.CreateClient();
    var response = await client.PostAsJsonAsync("/auth/login",
      new { Email = "admin@parkin.local", Password = "Admin!2345" });
    response.EnsureSuccessStatusCode();
    return client;
  }

  private static async Task<LotResponse> CreateLotAsync(HttpClient client, string? name = null,
    AccessMode accessMode = AccessMode.Open)
  {
    var response = await client.PostAsJsonAsync("/lots", new
    {
      Name = name ?? $"Lifecycle Lot {Guid.NewGuid():N}",
      Timezone = "Europe/Vilnius",
      AccessMode = accessMode.ToString(),
    });
    response.EnsureSuccessStatusCode();
    var lot = await response.Content.ReadFromJsonAsync<LotResponse>(JsonOptions);
    lot.ShouldNotBeNull();
    return lot;
  }

  private static async Task<List<JsonElement>> AuditEntriesAsync(HttpClient client, Guid lotId)
  {
    var response = await client.GetAsync("/audit?entity=ParkingLot&per_page=100");
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    return body.RootElement.GetProperty("items").EnumerateArray()
      .Where(entry => entry.GetProperty("entityId").GetGuid() == lotId)
      .ToList();
  }

  private static IEnumerable<string?> Actions(IEnumerable<JsonElement> entries) =>
    entries.Select(entry => entry.GetProperty("action").GetString());

  [Fact]
  public async Task Patch_RestrictedToOpen_AuditsAccessModeChangeOnly()
  {
    using var admin = await AdminClientAsync();
    var lot = await CreateLotAsync(admin, accessMode: AccessMode.Restricted);

    var response = await admin.PatchAsJsonAsync($"/lots/{lot.Id}", new { AccessMode = "Open" });

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    var entries = await AuditEntriesAsync(admin, lot.Id);
    Actions(entries).ShouldBe(["lot.access_mode_changed", "lot.created"], ignoreOrder: true);
    var metadataJson = entries.Single(e => e.GetProperty("action").GetString() == "lot.access_mode_changed")
      .GetProperty("metadataJson").GetString();
    var metadata = JsonDocument.Parse(metadataJson!).RootElement;
    metadata.GetProperty("from").GetString().ShouldBe("Restricted");
    metadata.GetProperty("to").GetString().ShouldBe("Open");
  }

  [Fact]
  public async Task Patch_WithUnchangedValues_WritesNoAudit()
  {
    using var admin = await AdminClientAsync();
    var lot = await CreateLotAsync(admin);

    var response = await admin.PatchAsJsonAsync($"/lots/{lot.Id}",
      new { lot.Name, lot.Timezone, AccessMode = lot.AccessMode.ToString(), FullBehavior = lot.FullBehavior.ToString() });

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    Actions(await AuditEntriesAsync(admin, lot.Id)).ShouldBe(["lot.created"]);
  }

  [Fact]
  public async Task Create_DuplicateName_Returns400()
  {
    using var admin = await AdminClientAsync();
    var lot = await CreateLotAsync(admin);

    var response = await admin.PostAsJsonAsync("/lots", new { lot.Name, Timezone = "Europe/Vilnius" });

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task Patch_RenameToExistingName_Returns400()
  {
    using var admin = await AdminClientAsync();
    var existing = await CreateLotAsync(admin);
    var lot = await CreateLotAsync(admin);

    var response = await admin.PatchAsJsonAsync($"/lots/{lot.Id}", new { existing.Name });

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task CreateSpace_DuplicateLabel_Returns400()
  {
    using var admin = await AdminClientAsync();
    var lot = await CreateLotAsync(admin);
    (await admin.PostAsJsonAsync($"/lots/{lot.Id}/spaces", new { Label = "A1" })).EnsureSuccessStatusCode();

    var response = await admin.PostAsJsonAsync($"/lots/{lot.Id}/spaces", new { Label = "A1" });

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task DeactivateSpace_UnknownSpace_Returns404()
  {
    using var admin = await AdminClientAsync();

    var response = await admin.PostAsync($"/spaces/{Guid.NewGuid()}/deactivate", content: null);

    response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
  }
}
