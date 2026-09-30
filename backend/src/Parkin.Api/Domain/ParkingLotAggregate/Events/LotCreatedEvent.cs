using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class LotCreatedEvent(ParkingLotId lotId, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.LotCreated, AuditEntityTypes.ParkingLot, lotId.Value)
{
  public ParkingLotId LotId { get; } = lotId;
}
