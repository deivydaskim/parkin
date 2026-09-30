using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class LotArchivedEvent(ParkingLotId lotId, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.LotArchived, AuditEntityTypes.ParkingLot, lotId.Value)
{
  public ParkingLotId LotId { get; } = lotId;
}
