using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Domain.AccessGrantAggregate.Events;

public class GrantCreatedEvent(AccessGrantId grantId, DriverId driverId, ParkingLotId lotId, Guid? actorId) : DomainEventBase
{
  public AccessGrantId GrantId { get; } = grantId;
  public DriverId DriverId { get; } = driverId;
  public ParkingLotId LotId { get; } = lotId;
  public Guid? ActorId { get; } = actorId;
}
