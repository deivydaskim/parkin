using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class SpaceDeactivatedEvent(ParkingLotId lotId, ParkingSpaceId spaceId, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.SpaceDeactivated, AuditEntityTypes.ParkingSpace, spaceId.Value)
{
  public ParkingLotId LotId { get; } = lotId;
  public ParkingSpaceId SpaceId { get; } = spaceId;
}
