namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class SpaceDeactivatedEvent(ParkingLotId lotId, ParkingSpaceId spaceId, Guid? actorId) : DomainEventBase
{
  public ParkingLotId LotId { get; } = lotId;
  public ParkingSpaceId SpaceId { get; } = spaceId;
  public Guid? ActorId { get; } = actorId;
}
