using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class SpaceReactivatedEvent(ParkingLotId lotId, ParkingSpaceId spaceId, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.SpaceReactivated, AuditEntityTypes.ParkingSpace, spaceId.Value)
{
  public ParkingLotId LotId { get; } = lotId;
  public ParkingSpaceId SpaceId { get; } = spaceId;

  public override object Metadata => new { lotId = LotId.Value };
}
