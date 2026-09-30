using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ReservationAggregate.Events;

namespace Parkin.Api.Domain.ReservationAggregate;

public class Reservation : EntityBase<Reservation, ReservationId>, IAggregateRoot
{
  public const string ActiveSpaceIndex = "ux_reservation_active_space";
  public const string ActiveDriverLotIndex = "ux_reservation_active_driver_lot";

  private Reservation() { }

  private Reservation(ReservationId id, ParkingSpaceId spaceId, DriverId driverId, ParkingLotId lotId)
  {
    Id = id;
    SpaceId = spaceId;
    DriverId = driverId;
    LotId = lotId;
    Status = ReservationStatus.Active;
  }

  public static Reservation Create(ParkingSpaceId spaceId, DriverId driverId, ParkingLotId lotId, Guid? actorId)
  {
    var reservation = new Reservation(ReservationId.From(Guid.CreateVersion7()), spaceId, driverId, lotId);
    reservation.RegisterDomainEvent(new ReservationCreatedEvent(reservation.Id, spaceId, driverId, lotId, actorId));
    return reservation;
  }

  public ParkingSpaceId SpaceId { get; private set; }
  public DriverId DriverId { get; private set; }
  public ParkingLotId LotId { get; private set; }
  public ReservationStatus Status { get; private set; }

  public Result Cancel(Guid? actorId)
  {
    var activeCheck = EnsureActive();
    if (!activeCheck.IsSuccess) return activeCheck;

    Status = ReservationStatus.Cancelled;
    RegisterDomainEvent(new ReservationCancelledEvent(Id, actorId));
    return Result.Success();
  }

  public Result<Reservation> ReassignTo(DriverId newDriverId, Guid? actorId)
  {
    var activeCheck = EnsureActive();
    if (!activeCheck.IsSuccess) return activeCheck;

    if (newDriverId == DriverId)
    {
      return Result.Invalid(new ValidationError("NewDriverId", "Reservation already belongs to this driver"));
    }

    Status = ReservationStatus.Cancelled;
    RegisterDomainEvent(new ReservationCancelledEvent(Id, actorId));

    var replacement = new Reservation(ReservationId.From(Guid.CreateVersion7()), SpaceId, newDriverId, LotId);
    replacement.RegisterDomainEvent(new ReservationReassignedEvent(
      replacement.Id, Id, SpaceId, LotId, DriverId, newDriverId, actorId));
    return replacement;
  }

  private Result EnsureActive()
    => Status == ReservationStatus.Active
      ? Result.Success()
      : Result.Invalid(new ValidationError("ReservationId", "Reservation is not active"));
}
