using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Domain.AccessGrantAggregate.Events;

public class GrantCreatedEvent(AccessGrantId grantId, DriverId driverId, ParkingLotId lotId, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.GrantCreated, AuditEntityTypes.AccessGrant, grantId.Value)
{
  public AccessGrantId GrantId { get; } = grantId;
  public DriverId DriverId { get; } = driverId;
  public ParkingLotId LotId { get; } = lotId;

  public override object Metadata => new { driverId = DriverId.Value, lotId = LotId.Value };
}
