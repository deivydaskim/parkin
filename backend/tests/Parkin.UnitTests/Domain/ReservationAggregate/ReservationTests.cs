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

  [Fact]
  public void Create_SetsFieldsAndDefaultsToActive()
  {
    var actorId = Guid.NewGuid();

    var reservation = Reservation.Create(SpaceId, DriverId, LotId, actorId);

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

    reservation.DomainEvents.ShouldContain(e => e is ReservationCreatedEvent
      && ((ReservationCreatedEvent)e).ReservationId == reservation.Id
      && ((ReservationCreatedEvent)e).SpaceId == SpaceId
      && ((ReservationCreatedEvent)e).DriverId == DriverId
      && ((ReservationCreatedEvent)e).LotId == LotId
      && ((ReservationCreatedEvent)e).ActorId == actorId);
  }

  [Fact]
  public void Cancel_FlipsStatusAndRegistersEvent()
  {
    var actorId = Guid.NewGuid();
    var reservation = Reservation.Create(SpaceId, DriverId, LotId, actorId: null);

    reservation.Cancel(actorId);

    reservation.Status.ShouldBe(ReservationStatus.Cancelled);
    reservation.DomainEvents.ShouldContain(e => e is ReservationCancelledEvent
      && ((ReservationCancelledEvent)e).ReservationId == reservation.Id
      && ((ReservationCancelledEvent)e).ActorId == actorId);
  }

  [Fact]
  public void CreateForReassignment_SetsFieldsAndDefaultsToActive()
  {
    var previousReservationId = ReservationId.From(Guid.NewGuid());
    var previousDriverId = DriverId.From(Guid.NewGuid());
    var actorId = Guid.NewGuid();

    var reservation = Reservation.CreateForReassignment(
      SpaceId, DriverId, LotId, previousReservationId, previousDriverId, actorId);

    reservation.SpaceId.ShouldBe(SpaceId);
    reservation.DriverId.ShouldBe(DriverId);
    reservation.LotId.ShouldBe(LotId);
    reservation.Status.ShouldBe(ReservationStatus.Active);
  }

  [Fact]
  public void CreateForReassignment_RegistersOnlyReservationReassignedEvent()
  {
    var previousReservationId = ReservationId.From(Guid.NewGuid());
    var previousDriverId = DriverId.From(Guid.NewGuid());
    var actorId = Guid.NewGuid();

    var reservation = Reservation.CreateForReassignment(
      SpaceId, DriverId, LotId, previousReservationId, previousDriverId, actorId);

    reservation.DomainEvents.Count.ShouldBe(1);
    reservation.DomainEvents.ShouldContain(e => e is ReservationReassignedEvent
      && ((ReservationReassignedEvent)e).NewReservationId == reservation.Id
      && ((ReservationReassignedEvent)e).PreviousReservationId == previousReservationId
      && ((ReservationReassignedEvent)e).SpaceId == SpaceId
      && ((ReservationReassignedEvent)e).LotId == LotId
      && ((ReservationReassignedEvent)e).PreviousDriverId == previousDriverId
      && ((ReservationReassignedEvent)e).NewDriverId == DriverId
      && ((ReservationReassignedEvent)e).ActorId == actorId);
  }
}
