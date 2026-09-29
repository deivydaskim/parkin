using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Features.Lots;
using Parkin.Api.Features.Spaces;

namespace Parkin.Api.Features.LotLayouts;

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
