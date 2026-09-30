using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Features.Lots;
using Parkin.Api.Features.Spaces;

namespace Parkin.Api.Features.LotLayouts;

public record LotLayoutViewResponse(LotLayoutLotResponse Lot, IReadOnlyList<LotLayoutSpaceResponse> Spaces);

public record LotLayoutLotResponse(Guid Id, string Name, LotStatus Status, LotLayoutResponse? Layout);

public record LotLayoutSpaceResponse(
  Guid Id,
  string Label,
  SpaceType Type,
  SpaceStatus Status,
  string? Zone,
  SpacePlacementResponse? Placement,
  LotLayoutReservationResponse? Reservation);

public record LotLayoutReservationResponse(Guid Id, Guid DriverId, string DriverName);
