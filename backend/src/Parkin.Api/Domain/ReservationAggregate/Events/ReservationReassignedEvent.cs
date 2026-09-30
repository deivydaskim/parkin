using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Domain.ReservationAggregate.Events;

public class ReservationReassignedEvent(ReservationId newReservationId, ReservationId previousReservationId, ParkingSpaceId spaceId,
  ParkingLotId lotId, DriverId previousDriverId, DriverId newDriverId, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.ReservationReassigned, AuditEntityTypes.Reservation, newReservationId.Value)
{
  public ReservationId NewReservationId { get; } = newReservationId;
  public ReservationId PreviousReservationId { get; } = previousReservationId;
  public ParkingSpaceId SpaceId { get; } = spaceId;
  public ParkingLotId LotId { get; } = lotId;
  public DriverId PreviousDriverId { get; } = previousDriverId;
  public DriverId NewDriverId { get; } = newDriverId;

  public override object Metadata => new { previousReservationId = PreviousReservationId.Value, spaceId = SpaceId.Value, lotId = LotId.Value, previousDriverId = PreviousDriverId.Value, newDriverId = NewDriverId.Value };
}
