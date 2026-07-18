using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Domain.ReservationAggregate.Events;

public class ReservationCreatedEvent(ReservationId reservationId, ParkingSpaceId spaceId, DriverId driverId, ParkingLotId lotId, Guid? actorId) : DomainEventBase
{
  public ReservationId ReservationId { get; } = reservationId;
  public ParkingSpaceId SpaceId { get; } = spaceId;
  public DriverId DriverId { get; } = driverId;
  public ParkingLotId LotId { get; } = lotId;
  public Guid? ActorId { get; } = actorId;
}
