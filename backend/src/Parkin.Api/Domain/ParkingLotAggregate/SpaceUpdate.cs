namespace Parkin.Api.Domain.ParkingLotAggregate;

public sealed record SpaceUpdate(
  string? Label = null,
  SpaceType? Type = null,
  string? Zone = null,
  SpacePlacement? Placement = null,
  bool ClearPlacement = false);
