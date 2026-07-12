namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class LotArchivedEvent(ParkingLotId lotId) : DomainEventBase
{
  public ParkingLotId LotId { get; } = lotId;
}
