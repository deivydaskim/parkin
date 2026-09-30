using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class LotUpdatedEvent(ParkingLotId lotId, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.LotUpdated, AuditEntityTypes.ParkingLot, lotId.Value)
{
  public ParkingLotId LotId { get; } = lotId;
}
