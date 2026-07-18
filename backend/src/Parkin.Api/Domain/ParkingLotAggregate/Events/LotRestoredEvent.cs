namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class LotRestoredEvent(ParkingLotId lotId, Guid? actorId) : DomainEventBase
{
  public ParkingLotId LotId { get; } = lotId;
  public Guid? ActorId { get; } = actorId;
}
