namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class LotUpdatedEvent(ParkingLotId lotId, Guid? actorId) : DomainEventBase
{
  public ParkingLotId LotId { get; } = lotId;
  public Guid? ActorId { get; } = actorId;
}
