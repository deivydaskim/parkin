using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class SpaceUpdatedEvent(ParkingLotId lotId, ParkingSpaceId spaceId, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.SpaceUpdated, AuditEntityTypes.ParkingSpace, spaceId.Value)
{
  public ParkingLotId LotId { get; } = lotId;
  public ParkingSpaceId SpaceId { get; } = spaceId;
}
