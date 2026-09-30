using Parkin.Api.Domain.AccessGrantAggregate;
using Parkin.Api.Domain.AccessGrantAggregate.Specifications;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Domain.ReservationAggregate.Specifications;
using Parkin.Api.Domain.Services;

namespace Parkin.Api.Features.AccessEvents.Ingest;

public sealed record PlateMatch(PlateId? PlateId, DriverId? DriverId, bool IsKnown)
{
  public static readonly PlateMatch None = new(null, null, false);

  public DriverId? KnownDriverId => IsKnown ? DriverId : null;
}

public sealed record EntryContext(EntryDecisionContext DecisionContext, ParkingSpaceId? ReservedSpaceId);

public class EntryContextBuilder(
  IReadRepository<Driver> driverRepository,
  IReadRepository<AccessGrant> grantRepository,
  IReadRepository<Reservation> reservationRepository,
  IGateLotReader gateLotReader)
{
  public async Task<PlateMatch> MatchPlateAsync(string normalizedPlate, CancellationToken cancellationToken)
  {
    var driver = await driverRepository.FirstOrDefaultAsync(
      new PlateByNormalizedValueSpec(normalizedPlate), cancellationToken);
    var plate = driver?.Plates.FirstOrDefault(p => p.NormalizedPlateNumber == normalizedPlate);

    return driver is null || plate is null
      ? PlateMatch.None
      : new PlateMatch(plate.Id, driver.Id, IsKnown: plate.Status == PlateStatus.Active);
  }

  public async Task<EntryContext> BuildAsync(GateLot lot, PlateMatch plate, DateTimeOffset occurredAt,
    CancellationToken cancellationToken)
  {
    var occupancy = OccupancyResult.Calculate(
      OccupancyContext.Create(lot.GeneralCapacity, lot.ActiveGeneralSessions, lot.ActiveReservedSessions));

    var driverId = plate.KnownDriverId;
    var hasActiveGrant = driverId is not null &&
      await HasGrantActiveAtAsync(driverId.Value, lot.Id, occurredAt, cancellationToken);
    var reservedSpace = driverId is null
      ? null
      : await FindReservedSpaceAsync(driverId.Value, lot.Id, cancellationToken);

    var decisionContext = EntryDecisionContext.Create(
      plate.IsKnown,
      lot.AccessMode,
      lot.FullBehavior,
      hasActiveGrant,
      hasActiveReservation: reservedSpace is not null,
      occupancy.IsGeneralPoolFull,
      reservedSpace?.Label);

    return new EntryContext(decisionContext, reservedSpace?.Id);
  }

  private async Task<bool> HasGrantActiveAtAsync(DriverId driverId, ParkingLotId lotId, DateTimeOffset occurredAt,
    CancellationToken cancellationToken)
  {
    var grants = await grantRepository.ListAsync(new ActiveGrantForDriverLotSpec(driverId, lotId), cancellationToken);
    return grants.Any(grant => grant.IsActiveAsOf(occurredAt));
  }

  private async Task<ReservedSpace?> FindReservedSpaceAsync(DriverId driverId, ParkingLotId lotId,
    CancellationToken cancellationToken)
  {
    var reservation = await reservationRepository.FirstOrDefaultAsync(
      new ActiveReservationByDriverLotSpec(driverId, lotId), cancellationToken);
    if (reservation is null) return null;

    var label = await gateLotReader.FindActiveSpaceLabelAsync(lotId, reservation.SpaceId, cancellationToken);
    return label is null ? null : new ReservedSpace(reservation.SpaceId, label);
  }

  private sealed record ReservedSpace(ParkingSpaceId Id, string Label);
}
