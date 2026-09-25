using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Parkin.Api.AuditFeatures;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.DriverFeatures;
using Parkin.Api.LotFeatures;
using Parkin.Api.LotLayoutFeatures;
using Parkin.Api.ReservationFeatures;
using Parkin.Api.SpaceFeatures;
using Shouldly;
using Xunit;

namespace Parkin.FunctionalTests.LotLayoutFeatures;

public class LotLayoutTests : IClassFixture<ParkinApiFactory>
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
  {
    Converters = { new JsonStringEnumConverter() },
  };

  private readonly ParkinApiFactory _factory;

  public LotLayoutTests(ParkinApiFactory factory) => _factory = factory;

  private static string LayoutRoute(Guid lotId) => $"/lots/{lotId}/layout";

  private async Task<HttpClient> AdminClientAsync()
  {
    var client = _factory.CreateClient();
    var response = await client.PostAsJsonAsync("/auth/login",
      new { Email = "admin@parkin.local", Password = "Admin!2345" });
    response.EnsureSuccessStatusCode();
    return client;
  }

  private static async Task<LotRecord> CreateLotAsync(HttpClient client, object? layout = null)
  {
    var response = await client.PostAsJsonAsync("/lots", new
    {
      Name = $"Layout Lot {Guid.NewGuid():N}",
      Timezone = "Europe/Vilnius",
      Layout = layout,
    });
    response.EnsureSuccessStatusCode();
    var lot = await response.Content.ReadFromJsonAsync<LotRecord>(JsonOptions);
    lot.ShouldNotBeNull();
    return lot;
  }

  private static async Task<SpaceRecord> CreateSpaceAsync(HttpClient client, Guid lotId, string label,
    SpaceType type = SpaceType.General, object? placement = null, string? zone = null)
  {
    var response = await client.PostAsJsonAsync($"/lots/{lotId}/spaces",
      new { Label = label, Type = type.ToString(), Placement = placement, Zone = zone });
    response.EnsureSuccessStatusCode();
    var space = await response.Content.ReadFromJsonAsync<SpaceRecord>(JsonOptions);
    space.ShouldNotBeNull();
    return space;
  }

  private static async Task<LotLayoutViewRecord> GetLayoutAsync(HttpClient client, Guid lotId)
  {
    var response = await client.GetAsync(LayoutRoute(lotId));
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    var view = await response.Content.ReadFromJsonAsync<LotLayoutViewRecord>(JsonOptions);
    view.ShouldNotBeNull();
    return view;
  }

  private static async Task<Guid> CreateDriverAsync(HttpClient client, string name)
  {
    var response = await client.PostAsJsonAsync("/drivers", new { Name = name });
    response.EnsureSuccessStatusCode();
    var driver = await response.Content.ReadFromJsonAsync<DriverRecord>(JsonOptions);
    driver.ShouldNotBeNull();
    return driver.Id;
  }

  [Fact]
  public async Task Get_Unauthenticated_Returns401()
  {
    using var client = _factory.CreateClient();

    var response = await client.GetAsync(LayoutRoute(Guid.NewGuid()));

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Put_Unauthenticated_Returns401()
  {
    using var client = _factory.CreateClient();

    var response = await client.PutAsJsonAsync(LayoutRoute(Guid.NewGuid()), new { Spaces = Array.Empty<object>() });

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Get_UnknownLot_Returns404()
  {
    using var admin = await AdminClientAsync();

    var response = await admin.GetAsync(LayoutRoute(Guid.NewGuid()));

    response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task CreateSpace_WithPlacementAndZone_RoundTrips()
  {
    using var admin = await AdminClientAsync();
    var lot = await CreateLotAsync(admin, new { WidthMeters = 40, LengthMeters = 30, LevelCount = 2 });

    var space = await CreateSpaceAsync(admin, lot.Id, "A1",
      placement: new { X = 12.5, Y = 6, RotationDegrees = 90, Level = 1 }, zone: " North ");

    lot.Layout.ShouldBe(new LotLayoutRecord(40m, 30m, 2));
    space.Zone.ShouldBe("North");
    space.Placement.ShouldBe(new SpacePlacementRecord(12.5m, 6m, 90m, 1, 2.5m, 5m));
  }

  [Fact]
  public async Task CreateSpace_PlacementOutsideFootprint_Returns400()
  {
    using var admin = await AdminClientAsync();
    var lot = await CreateLotAsync(admin, new { WidthMeters = 10, LengthMeters = 10, LevelCount = 1 });

    var response = await admin.PostAsJsonAsync($"/lots/{lot.Id}/spaces",
      new { Label = "A1", Type = "General", Placement = new { X = 20, Y = 5 } });

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task PatchSpace_MovesRotatesZonesAndUnplaces()
  {
    using var admin = await AdminClientAsync();
    var lot = await CreateLotAsync(admin);
    var space = await CreateSpaceAsync(admin, lot.Id, "A1");
    space.Placement.ShouldBeNull();

    var place = await admin.PatchAsJsonAsync($"/spaces/{space.Id}",
      new { Placement = new { X = 3, Y = 4, RotationDegrees = 45, Width = 2.6, Length = 5.2 }, Zone = "East" });
    place.StatusCode.ShouldBe(HttpStatusCode.OK);
    var placed = await place.Content.ReadFromJsonAsync<SpaceRecord>(JsonOptions);
    placed!.Placement.ShouldBe(new SpacePlacementRecord(3m, 4m, 45m, 0, 2.6m, 5.2m));
    placed.Zone.ShouldBe("East");

    var rename = await admin.PatchAsJsonAsync($"/spaces/{space.Id}", new { Label = "A1-renamed" });
    var renamed = await rename.Content.ReadFromJsonAsync<SpaceRecord>(JsonOptions);
    renamed!.Placement.ShouldNotBeNull();
    renamed.Zone.ShouldBe("East");

    var clear = await admin.PatchAsJsonAsync($"/spaces/{space.Id}", new { ClearPlacement = true, Zone = "" });
    var cleared = await clear.Content.ReadFromJsonAsync<SpaceRecord>(JsonOptions);
    cleared!.Placement.ShouldBeNull();
    cleared.Zone.ShouldBeNull();
  }

  [Fact]
  public async Task PatchSpace_InvalidRotation_Returns400()
  {
    using var admin = await AdminClientAsync();
    var lot = await CreateLotAsync(admin);
    var space = await CreateSpaceAsync(admin, lot.Id, "A1");

    var response = await admin.PatchAsJsonAsync($"/spaces/{space.Id}",
      new { Placement = new { X = 3, Y = 4, RotationDegrees = 360 } });

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task PatchLot_SetsAndClearsLayout()
  {
    using var admin = await AdminClientAsync();
    var lot = await CreateLotAsync(admin);

    var set = await admin.PatchAsJsonAsync($"/lots/{lot.Id}",
      new { Layout = new { WidthMeters = 55.5, LengthMeters = 20, LevelCount = 3 } });
    var withLayout = await set.Content.ReadFromJsonAsync<LotRecord>(JsonOptions);
    withLayout!.Layout.ShouldBe(new LotLayoutRecord(55.5m, 20m, 3));

    var clear = await admin.PatchAsJsonAsync($"/lots/{lot.Id}", new { ClearLayout = true });
    var withoutLayout = await clear.Content.ReadFromJsonAsync<LotRecord>(JsonOptions);
    withoutLayout!.Layout.ShouldBeNull();
  }

  [Fact]
  public async Task PatchLot_LayoutThatStrandsAPlacedSpace_Returns400()
  {
    using var admin = await AdminClientAsync();
    var lot = await CreateLotAsync(admin);
    await CreateSpaceAsync(admin, lot.Id, "A1", placement: new { X = 30, Y = 5 });

    var response = await admin.PatchAsJsonAsync($"/lots/{lot.Id}",
      new { Layout = new { WidthMeters = 20, LengthMeters = 20, LevelCount = 1 } });

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task Put_AppliesBatch_ReturnsProjection_AndAuditsOnce()
  {
    using var admin = await AdminClientAsync();
    var lot = await CreateLotAsync(admin);
    var first = await CreateSpaceAsync(admin, lot.Id, "A1");
    var second = await CreateSpaceAsync(admin, lot.Id, "A2", placement: new { X = 1, Y = 1 });
    var untouched = await CreateSpaceAsync(admin, lot.Id, "A3", placement: new { X = 9, Y = 9 });

    var response = await admin.PutAsJsonAsync(LayoutRoute(lot.Id), new
    {
      Layout = new { WidthMeters = 30, LengthMeters = 20, LevelCount = 2 },
      Spaces = new object[]
      {
        new { SpaceId = first.Id, Placement = new { X = 5, Y = 3.5, RotationDegrees = 0, Level = 1 }, Zone = "Upper" },
        new { SpaceId = second.Id, Placement = (object?)null },
      },
    });

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    var view = await response.Content.ReadFromJsonAsync<LotLayoutViewRecord>(JsonOptions);
    view.ShouldNotBeNull();
    view.Lot.Layout.ShouldBe(new LotLayoutRecord(30m, 20m, 2));
    view.Spaces.Single(s => s.Id == first.Id).Placement.ShouldBe(new SpacePlacementRecord(5m, 3.5m, 0m, 1, 2.5m, 5m));
    view.Spaces.Single(s => s.Id == first.Id).Zone.ShouldBe("Upper");
    view.Spaces.Single(s => s.Id == second.Id).Placement.ShouldBeNull();
    view.Spaces.Single(s => s.Id == untouched.Id).Placement.ShouldBe(new SpacePlacementRecord(9m, 9m, 0m, 0, 2.5m, 5m));

    var audit = await admin.GetAsync($"/audit?entity=ParkingLot&per_page=100");
    audit.StatusCode.ShouldBe(HttpStatusCode.OK);
    var auditBody = await audit.Content.ReadAsStringAsync();
    var entries = JsonDocument.Parse(auditBody).RootElement.GetProperty("items").EnumerateArray()
      .Where(e => e.GetProperty("entityId").GetGuid() == lot.Id
        && e.GetProperty("action").GetString() == "lot.layout_applied")
      .ToList();
    entries.Count.ShouldBe(1);
  }

  [Fact]
  public async Task Put_OneRowOutsideFootprint_IsAllOrNothing()
  {
    using var admin = await AdminClientAsync();
    var lot = await CreateLotAsync(admin);
    var first = await CreateSpaceAsync(admin, lot.Id, "A1");
    var second = await CreateSpaceAsync(admin, lot.Id, "A2");

    var response = await admin.PutAsJsonAsync(LayoutRoute(lot.Id), new
    {
      Layout = new { WidthMeters = 10, LengthMeters = 10, LevelCount = 1 },
      Spaces = new object[]
      {
        new { SpaceId = first.Id, Placement = new { X = 5, Y = 5 } },
        new { SpaceId = second.Id, Placement = new { X = 50, Y = 5 } },
      },
    });

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    var view = await GetLayoutAsync(admin, lot.Id);
    view.Lot.Layout.ShouldBeNull();
    view.Spaces.ShouldAllBe(s => s.Placement == null);
  }

  [Fact]
  public async Task Put_ForeignSpaceId_Returns400()
  {
    using var admin = await AdminClientAsync();
    var lot = await CreateLotAsync(admin);
    var otherLot = await CreateLotAsync(admin);
    var foreign = await CreateSpaceAsync(admin, otherLot.Id, "F1");

    var response = await admin.PutAsJsonAsync(LayoutRoute(lot.Id), new
    {
      Spaces = new object[] { new { SpaceId = foreign.Id, Placement = new { X = 5, Y = 5 } } },
    });

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task Put_UnknownLot_Returns404()
  {
    using var admin = await AdminClientAsync();

    var response = await admin.PutAsJsonAsync(LayoutRoute(Guid.NewGuid()), new { Spaces = Array.Empty<object>() });

    response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task Get_IncludesInactiveAndUnplacedSpaces_AndReservationAssignee()
  {
    using var admin = await AdminClientAsync();
    var lot = await CreateLotAsync(admin);
    var reserved = await CreateSpaceAsync(admin, lot.Id, "R1", SpaceType.Reserved, new { X = 4, Y = 4 });
    var inactive = await CreateSpaceAsync(admin, lot.Id, "G1", placement: new { X = 8, Y = 4 });
    var unplaced = await CreateSpaceAsync(admin, lot.Id, "G2");
    (await admin.PostAsync($"/spaces/{inactive.Id}/deactivate", content: null)).EnsureSuccessStatusCode();

    var driverName = $"Driver {Guid.NewGuid():N}";
    var driverId = await CreateDriverAsync(admin, driverName);
    var reserve = await admin.PostAsJsonAsync("/reservations", new { SpaceId = reserved.Id, DriverId = driverId });
    reserve.EnsureSuccessStatusCode();
    var reservation = await reserve.Content.ReadFromJsonAsync<ReservationRecord>(JsonOptions);

    var view = await GetLayoutAsync(admin, lot.Id);

    view.Lot.Id.ShouldBe(lot.Id);
    view.Spaces.Count.ShouldBe(3);
    var reservedView = view.Spaces.Single(s => s.Id == reserved.Id);
    reservedView.Reservation.ShouldNotBeNull();
    reservedView.Reservation.DriverId.ShouldBe(driverId);
    reservedView.Reservation.DriverName.ShouldBe(driverName);
    view.Spaces.Single(s => s.Id == inactive.Id).Status.ShouldBe(SpaceStatus.Inactive);
    view.Spaces.Single(s => s.Id == unplaced.Id).Placement.ShouldBeNull();

    (await admin.PostAsync($"/reservations/{reservation!.Id}/cancel", content: null)).EnsureSuccessStatusCode();

    var afterCancel = await GetLayoutAsync(admin, lot.Id);
    afterCancel.Spaces.Single(s => s.Id == reserved.Id).Reservation.ShouldBeNull();
  }

  [Fact]
  public async Task Get_ArchivedLot_Returns200()
  {
    using var admin = await AdminClientAsync();
    var lot = await CreateLotAsync(admin);
    (await admin.PostAsync($"/lots/{lot.Id}/archive", content: null)).EnsureSuccessStatusCode();

    var view = await GetLayoutAsync(admin, lot.Id);

    view.Lot.Status.ShouldBe(LotStatus.Archived);
  }
}
