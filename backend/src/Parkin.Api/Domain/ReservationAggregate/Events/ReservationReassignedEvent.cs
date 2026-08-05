using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Domain.ReservationAggregate.Events;

public class ReservationReassignedEvent(
  ReservationId newReservationId,
  ReservationId previousReservationId,
  ParkingSpaceId spaceId,
  ParkingLotId lotId,
  DriverId previousDriverId,
  DriverId newDriverId,
  Guid? actorId) : DomainEventBase
{
  public ReservationId NewReservationId { get; } = newReservationId;
  public ReservationId PreviousReservationId { get; } = previousReservationId;
  public ParkingSpaceId SpaceId { get; } = spaceId;
  public ParkingLotId LotId { get; } = lotId;
  public DriverId PreviousDriverId { get; } = previousDriverId;
  public DriverId NewDriverId { get; } = newDriverId;
  public Guid? ActorId { get; } = actorId;
}
