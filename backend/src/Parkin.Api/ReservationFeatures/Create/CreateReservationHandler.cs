using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Domain.ReservationAggregate.Specifications;

namespace Parkin.Api.ReservationFeatures.Create;

public record CreateReservationCommand(
  ParkingSpaceId SpaceId,
  DriverId DriverId,
  Guid? ActorId) : ICommand<Result<ReservationDto>>;

public class CreateReservationHandler(
  IRepository<ParkingLot> lotRepository,
  IRepository<Driver> driverRepository,
  IRepository<Reservation> reservationRepository)
  : ICommandHandler<CreateReservationCommand, Result<ReservationDto>>
{
  public async ValueTask<Result<ReservationDto>> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
  {
    var lot = await lotRepository.FirstOrDefaultAsync(new ParkingLotBySpaceIdSpec(request.SpaceId), cancellationToken);
    if (lot == null) return Result.NotFound();

    var space = lot.Spaces.First(s => s.Id == request.SpaceId);
    if (space.Status != SpaceStatus.Active)
    {
      return Result.Invalid(new ValidationError("SpaceId", "Space is not active"));
    }

    var driver = await driverRepository.FirstOrDefaultAsync(new DriverByIdSpec(request.DriverId), cancellationToken);
    if (driver == null) return Result.NotFound();

    var existingSpaceReservation = await reservationRepository.FirstOrDefaultAsync(
      new ActiveReservationBySpaceSpec(request.SpaceId), cancellationToken);
    if (existingSpaceReservation != null)
    {
      return Result.Conflict("Space already has an active reservation");
    }

    var existingDriverLotReservation = await reservationRepository.FirstOrDefaultAsync(
      new ActiveReservationByDriverLotSpec(request.DriverId, lot.Id), cancellationToken);
    if (existingDriverLotReservation != null)
    {
      return Result.Conflict("Driver already has an active reservation in this lot");
    }

    var reservation = Reservation.Create(request.SpaceId, request.DriverId, lot.Id, request.ActorId);
    await reservationRepository.AddAsync(reservation, cancellationToken);

    lot.UpdateSpace(request.SpaceId, label: null, type: SpaceType.Reserved, request.ActorId);
    await lotRepository.UpdateAsync(lot, cancellationToken);

    return new ReservationDto(reservation.Id, reservation.SpaceId, reservation.DriverId, reservation.LotId,
      reservation.Status);
  }
}
