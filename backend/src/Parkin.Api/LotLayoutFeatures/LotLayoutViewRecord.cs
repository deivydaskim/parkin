using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.LotFeatures;
using Parkin.Api.SpaceFeatures;

namespace Parkin.Api.LotLayoutFeatures;

public record LotLayoutViewRecord(LotLayoutLotRecord Lot, IReadOnlyList<LotLayoutSpaceRecord> Spaces);

public record LotLayoutLotRecord(Guid Id, string Name, LotStatus Status, LotLayoutRecord? Layout);

public record LotLayoutSpaceRecord(
  Guid Id,
  string Label,
  SpaceType Type,
  SpaceStatus Status,
  string? Zone,
  SpacePlacementRecord? Placement,
  LotLayoutReservationRecord? Reservation);

public record LotLayoutReservationRecord(Guid Id, Guid DriverId, string DriverName);
