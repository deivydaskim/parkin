using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class LotRestoredEvent(ParkingLotId lotId, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.LotRestored, AuditEntityTypes.ParkingLot, lotId.Value)
{
  public ParkingLotId LotId { get; } = lotId;
}
