using Ardalis.Result;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Domain.ReservationAggregate.Events;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.Domain.ReservationAggregate;

public class ReservationTests
{
  private static readonly ParkingSpaceId SpaceId = ParkingSpaceId.From(Guid.NewGuid());
  private static readonly DriverId DriverId = DriverId.From(Guid.NewGuid());
  private static readonly ParkingLotId LotId = ParkingLotId.From(Guid.NewGuid());

  private static Reservation CreateActive() => Reservation.Create(SpaceId, DriverId, LotId, actorId: null);

  [Fact]
  public void Create_SetsFieldsAndDefaultsToActive()
  {
    var reservation = Reservation.Create(SpaceId, DriverId, LotId, Guid.NewGuid());

    reservation.SpaceId.ShouldBe(SpaceId);
    reservation.DriverId.ShouldBe(DriverId);
    reservation.LotId.ShouldBe(LotId);
    reservation.Status.ShouldBe(ReservationStatus.Active);
  }

  [Fact]
  public void Create_RegistersReservationCreatedEvent()
  {
    var actorId = Guid.NewGuid();

    var reservation = Reservation.Create(SpaceId, DriverId, LotId, actorId);

    var created = reservation.DomainEvents.OfType<ReservationCreatedEvent>().ShouldHaveSingleItem();
    created.ReservationId.ShouldBe(reservation.Id);
    created.SpaceId.ShouldBe(SpaceId);
    created.DriverId.ShouldBe(DriverId);
    created.LotId.ShouldBe(LotId);
    created.ActorId.ShouldBe(actorId);
  }

  [Fact]
  public void Cancel_Active_FlipsStatusAndRegistersEvent()
  {
    var actorId = Guid.NewGuid();
    var reservation = CreateActive();

    var result = reservation.Cancel(actorId);

    result.IsSuccess.ShouldBeTrue();
    reservation.Status.ShouldBe(ReservationStatus.Cancelled);
    var cancelled = reservation.DomainEvents.OfType<ReservationCancelledEvent>().ShouldHaveSingleItem();
    cancelled.ReservationId.ShouldBe(reservation.Id);
    cancelled.ActorId.ShouldBe(actorId);
  }

  [Fact]
  public void Cancel_AlreadyCancelled_ReturnsInvalidWithoutNewEvent()
  {
    var reservation = CreateActive();
    reservation.Cancel(actorId: null);

    var result = reservation.Cancel(actorId: null);

    result.Status.ShouldBe(ResultStatus.Invalid);
    reservation.DomainEvents.OfType<ReservationCancelledEvent>().Count().ShouldBe(1);
  }

  [Fact]
  public void ReassignTo_OtherDriver_CancelsSelfAndReturnsActiveReplacementForSameSpace()
  {
    var reservation = CreateActive();
    var newDriverId = DriverId.From(Guid.NewGuid());

    var result = reservation.ReassignTo(newDriverId, Guid.NewGuid());

    result.IsSuccess.ShouldBeTrue();
    reservation.Status.ShouldBe(ReservationStatus.Cancelled);
    var replacement = result.Value;
    replacement.Id.ShouldNotBe(reservation.Id);
    replacement.SpaceId.ShouldBe(SpaceId);
    replacement.LotId.ShouldBe(LotId);
    replacement.DriverId.ShouldBe(newDriverId);
    replacement.Status.ShouldBe(ReservationStatus.Active);
  }

  [Fact]
  public void ReassignTo_OtherDriver_ReplacementRegistersOnlyReservationReassignedEvent()
  {
    var reservation = CreateActive();
    var newDriverId = DriverId.From(Guid.NewGuid());
    var actorId = Guid.NewGuid();

    var replacement = reservation.ReassignTo(newDriverId, actorId).Value;

    var reassigned = replacement.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ReservationReassignedEvent>();
    reassigned.NewReservationId.ShouldBe(replacement.Id);
    reassigned.PreviousReservationId.ShouldBe(reservation.Id);
    reassigned.SpaceId.ShouldBe(SpaceId);
    reassigned.LotId.ShouldBe(LotId);
    reassigned.PreviousDriverId.ShouldBe(DriverId);
    reassigned.NewDriverId.ShouldBe(newDriverId);
    reassigned.ActorId.ShouldBe(actorId);
    reservation.DomainEvents.OfType<ReservationCancelledEvent>().ShouldHaveSingleItem();
  }

  [Fact]
  public void ReassignTo_SameDriver_ReturnsInvalidAndStaysActive()
  {
    var reservation = CreateActive();

    var result = reservation.ReassignTo(DriverId, actorId: null);

    result.Status.ShouldBe(ResultStatus.Invalid);
    reservation.Status.ShouldBe(ReservationStatus.Active);
  }

  [Fact]
  public void ReassignTo_CancelledReservation_ReturnsInvalid()
  {
    var reservation = CreateActive();
    reservation.Cancel(actorId: null);

    var result = reservation.ReassignTo(DriverId.From(Guid.NewGuid()), actorId: null);

    result.Status.ShouldBe(ResultStatus.Invalid);
  }
}
