namespace Parkin.Api.Domain.ParkingLotAggregate;

public sealed record SpaceLayoutChange(ParkingSpaceId SpaceId, SpacePlacement? Placement, string? Zone);
