using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Features.Drivers;
using Parkin.Api.Features.Grants;
using Parkin.Api.Features.Lots;
using Parkin.Api.Features.Plates;
using Parkin.Api.Features.Plates.List;
using Parkin.Api.Features.Reservations;
using Parkin.Api.Features.Spaces;
using Shouldly;
using Xunit;

namespace Parkin.FunctionalTests.DriverFeatures;

public class DriverLifecycleTests : IClassFixture<ParkinApiFactory>
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
  {
    Converters = { new JsonStringEnumConverter() },
  };

  private readonly ParkinApiFactory _factory;

  public DriverLifecycleTests(ParkinApiFactory factory) => _factory = factory;

  private static string Unique() => Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

  private async Task<HttpClient> CreateOperatorClientAsync()
  {
    var client = _factory.CreateClient();
    var response = await client.PostAsJsonAsync("/auth/login",
      new { Email = "operator@parkin.local", Password = "Operator!2345" });
    response.EnsureSuccessStatusCode();
    return client;
  }

  private static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expected) where T : class
  {
    response.StatusCode.ShouldBe(expected);
    var body = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    body.ShouldNotBeNull();
    return body;
  }

  private static async Task<DriverResponse> CreateDriverAsync(HttpClient client) =>
    await ReadAsync<DriverResponse>(
      await client.PostAsJsonAsync("/drivers", new { Name = $"Driver {Unique()}" }), HttpStatusCode.Created);

  private static async Task<PlateResponse> AddPlateAsync(HttpClient client, Guid driverId, string plate) =>
    await ReadAsync<PlateResponse>(
      await client.PostAsJsonAsync($"/drivers/{driverId}/plates", new { PlateNumber = plate }), HttpStatusCode.Created);

  private static async Task<LotResponse> CreateLotAsync(HttpClient client) =>
    await ReadAsync<LotResponse>(await client.PostAsJsonAsync("/lots",
      new { Name = $"Lifecycle Lot {Unique()}", Timezone = "Europe/Vilnius", AccessMode = "Restricted" }),
      HttpStatusCode.Created);

  [Fact]
  public async Task PlateOperations_OnUnknownPlate_Return404()
  {
    using var client = await CreateOperatorClientAsync();
    var unknownPlateId = Guid.NewGuid();

    (await client.PostAsync($"/plates/{unknownPlateId}/deactivate", null)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    (await client.PostAsync($"/plates/{unknownPlateId}/reactivate", null)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task Plates_AddDeactivateReactivateAndList_RoundTrips()
  {
    using var client = await CreateOperatorClientAsync();
    var driver = await CreateDriverAsync(client);
    var number = $"LT{Unique()}";
    var plate = await AddPlateAsync(client, driver.Id, number.ToLowerInvariant());

    plate.NormalizedPlateNumber.ShouldBe(number);
    plate.DriverId.ShouldBe(driver.Id);

    var deactivated = await ReadAsync<PlateResponse>(
      await client.PostAsync($"/plates/{plate.Id}/deactivate", null), HttpStatusCode.OK);
    deactivated.Status.ShouldBe(PlateStatus.Inactive);

    var reactivated = await ReadAsync<PlateResponse>(
      await client.PostAsync($"/plates/{plate.Id}/reactivate", null), HttpStatusCode.OK);
    reactivated.Status.ShouldBe(PlateStatus.Active);

    var list = await ReadAsync<PlateListResponse>(
      await client.GetAsync($"/drivers/{driver.Id}/plates"), HttpStatusCode.OK);
    list.Items.ShouldHaveSingleItem().Id.ShouldBe(plate.Id);
  }

  [Fact]
  public async Task AddPlate_DuplicateNumber_Returns400()
  {
    using var client = await CreateOperatorClientAsync();
    var first = await CreateDriverAsync(client);
    var second = await CreateDriverAsync(client);
    var number = $"DP{Unique()}";
    await AddPlateAsync(client, first.Id, number);

    var response = await client.PostAsJsonAsync($"/drivers/{second.Id}/plates", new { PlateNumber = number });

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task ReassignPlate_MovesPlateAndRejectsCurrentOwner()
  {
    using var client = await CreateOperatorClientAsync();
    var source = await CreateDriverAsync(client);
    var target = await CreateDriverAsync(client);
    var plate = await AddPlateAsync(client, source.Id, $"RA{Unique()}");

    var toSelf = await client.PostAsJsonAsync($"/plates/{plate.Id}/reassign", new { TargetDriverId = source.Id });
    toSelf.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

    var moved = await ReadAsync<PlateResponse>(
      await client.PostAsJsonAsync($"/plates/{plate.Id}/reassign", new { TargetDriverId = target.Id }),
      HttpStatusCode.OK);
    moved.DriverId.ShouldBe(target.Id);

    var targetDriver = await ReadAsync<DriverResponse>(await client.GetAsync($"/drivers/{target.Id}"), HttpStatusCode.OK);
    targetDriver.PlateCount.ShouldBe(1);
  }

  [Fact]
  public async Task CreateGrant_ValidToAlreadyPassedWithoutValidFrom_Returns400()
  {
    using var client = await CreateOperatorClientAsync();
    var lot = await CreateLotAsync(client);
    var driver = await CreateDriverAsync(client);

    var response = await client.PostAsJsonAsync("/grants",
      new { DriverId = driver.Id, LotId = lot.Id, ValidTo = DateTimeOffset.UtcNow.AddDays(-1) });

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task RevokeGrant_Twice_SecondReturns400()
  {
    using var client = await CreateOperatorClientAsync();
    var lot = await CreateLotAsync(client);
    var driver = await CreateDriverAsync(client);
    var grant = await ReadAsync<GrantResponse>(
      await client.PostAsJsonAsync("/grants", new { DriverId = driver.Id, LotId = lot.Id }), HttpStatusCode.Created);
    grant.ParkingLotName.ShouldBe(lot.Name);

    (await client.PostAsync($"/grants/{grant.Id}/revoke", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
    (await client.PostAsync($"/grants/{grant.Id}/revoke", null)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task Reservation_CreateReassignAndCancel_EnforcesActiveState()
  {
    using var client = await CreateOperatorClientAsync();
    var lot = await CreateLotAsync(client);
    var space = await ReadAsync<SpaceResponse>(
      await client.PostAsJsonAsync($"/lots/{lot.Id}/spaces", new { Label = "R1", Type = "General" }),
      HttpStatusCode.Created);
    var firstDriver = await CreateDriverAsync(client);
    var secondDriver = await CreateDriverAsync(client);

    var reservation = await ReadAsync<ReservationResponse>(
      await client.PostAsJsonAsync("/reservations", new { SpaceId = space.Id, DriverId = firstDriver.Id }),
      HttpStatusCode.Created);

    var duplicate = await client.PostAsJsonAsync("/reservations", new { SpaceId = space.Id, DriverId = secondDriver.Id });
    duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);

    var reassigned = await ReadAsync<ReservationResponse>(
      await client.PostAsJsonAsync($"/reservations/{reservation.Id}/reassign", new { DriverId = secondDriver.Id }),
      HttpStatusCode.OK);
    reassigned.DriverId.ShouldBe(secondDriver.Id);
    reassigned.SpaceId.ShouldBe(space.Id);

    (await client.PostAsync($"/reservations/{reservation.Id}/cancel", null)).StatusCode
      .ShouldBe(HttpStatusCode.BadRequest);

    var cancelled = await ReadAsync<ReservationResponse>(
      await client.PostAsync($"/reservations/{reassigned.Id}/cancel", null), HttpStatusCode.OK);
    cancelled.Status.ShouldBe(ReservationStatus.Cancelled);

    (await client.GetAsync($"/spaces/{space.Id}/reservation")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
  }
}
