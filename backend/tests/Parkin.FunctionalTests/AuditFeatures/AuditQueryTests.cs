using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Parkin.Api.AuditFeatures.List;
using Parkin.Api.LotFeatures;
using Parkin.Api.LotFeatures.Create;
using Shouldly;
using Xunit;

namespace Parkin.FunctionalTests.AuditFeatures;

public class AuditQueryTests : IClassFixture<ParkinApiFactory>
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
  {
    Converters = { new JsonStringEnumConverter() },
  };

  private readonly ParkinApiFactory _factory;

  public AuditQueryTests(ParkinApiFactory factory) => _factory = factory;

  private static async Task LoginAsync(HttpClient client, string email, string password)
  {
    var response = await client.PostAsJsonAsync("/auth/login", new { Email = email, Password = password });
    response.EnsureSuccessStatusCode();
  }

  [Fact]
  public async Task Get_Audit_AsOperator_Returns403()
  {
    using var client = _factory.CreateClient();
    await LoginAsync(client, "operator@parkin.local", "Operator!2345");

    var response = await client.GetAsync("/audit");

    response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
  }

  [Fact]
  public async Task Get_Audit_Unauthenticated_Returns401()
  {
    using var client = _factory.CreateClient();

    var response = await client.GetAsync("/audit");

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Get_Audit_AsAdmin_ReturnsLotCreationEntry_AndFiltersByEntityType()
  {
    using var client = _factory.CreateClient();
    await LoginAsync(client, "admin@parkin.local", "Admin!2345");

    var lotName = $"Audit Test Lot {Guid.NewGuid():N}";
    var createResponse = await client.PostAsJsonAsync("/lots", new CreateLotRequest
    {
      Name = lotName,
      Timezone = "America/New_York",
    });
    createResponse.EnsureSuccessStatusCode();
    var lot = await createResponse.Content.ReadFromJsonAsync<LotRecord>(JsonOptions);
    lot.ShouldNotBeNull();

    var auditResponse = await client.GetAsync($"/audit?entity=ParkingLot&per_page=100");
    auditResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

    var page = await auditResponse.Content.ReadFromJsonAsync<AuditListResponse>(JsonOptions);
    page.ShouldNotBeNull();
    page.Items.ShouldContain(e => e.EntityId == lot.Id && e.Action == "lot.created" && e.EntityType == "ParkingLot");
  }

  [Fact]
  public async Task Get_Audit_FiltersOutEntriesOutsideDateRange()
  {
    using var client = _factory.CreateClient();
    await LoginAsync(client, "admin@parkin.local", "Admin!2345");

    var createResponse = await client.PostAsJsonAsync("/lots", new CreateLotRequest
    {
      Name = $"Audit Range Lot {Guid.NewGuid():N}",
      Timezone = "America/New_York",
    });
    createResponse.EnsureSuccessStatusCode();
    var lot = await createResponse.Content.ReadFromJsonAsync<LotRecord>(JsonOptions);
    lot.ShouldNotBeNull();

    var future = DateTimeOffset.UtcNow.AddDays(1).ToString("O");
    var auditResponse = await client.GetAsync($"/audit?entity=ParkingLot&from={Uri.EscapeDataString(future)}&per_page=100");
    auditResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

    var page = await auditResponse.Content.ReadFromJsonAsync<AuditListResponse>(JsonOptions);
    page.ShouldNotBeNull();
    page.Items.ShouldNotContain(e => e.EntityId == lot.Id);
  }
}
