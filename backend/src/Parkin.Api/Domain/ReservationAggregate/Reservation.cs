using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ReservationAggregate.Events;

namespace Parkin.Api.Domain.ReservationAggregate;

public class Reservation : EntityBase<Reservation, ReservationId>, IAggregateRoot
{
  // Private constructor for EF Core
  private Reservation() { }

  private Reservation(ReservationId id, ParkingSpaceId spaceId, DriverId driverId, ParkingLotId lotId)
  {
    Id = id;
    SpaceId = spaceId;
    DriverId = driverId;
    LotId = lotId;
    Status = ReservationStatus.Active;
  }

  // Factory method for creating new reservations (before persistence)
  public static Reservation Create(ParkingSpaceId spaceId, DriverId driverId, ParkingLotId lotId, Guid? actorId)
  {
    var reservation = new Reservation(ReservationId.From(Guid.NewGuid()), spaceId, driverId, lotId);
    reservation.RegisterDomainEvent(new ReservationCreatedEvent(reservation.Id, spaceId, driverId, lotId, actorId));
    return reservation;
  }

  public ParkingSpaceId SpaceId { get; private set; }
  public DriverId DriverId { get; private set; }
  public ParkingLotId LotId { get; private set; }
  public ReservationStatus Status { get; private set; }

  public void Cancel(Guid? actorId)
  {
    Status = ReservationStatus.Cancelled;
    RegisterDomainEvent(new ReservationCancelledEvent(Id, actorId));
  }
}
