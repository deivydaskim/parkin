using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;
using Parkin.Api.Domain.Interfaces;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Domain.ReservationAggregate.Specifications;

namespace Parkin.Api.ReservationFeatures.Reassign;

public record ReassignReservationCommand(
  ReservationId ReservationId,
  DriverId NewDriverId,
  Guid? ActorId) : ICommand<Result<ReservationDto>>;

public class ReassignReservationHandler(
  IRepository<Reservation> reservationRepository,
  IRepository<Driver> driverRepository,
  IUnitOfWork unitOfWork)
  : ICommandHandler<ReassignReservationCommand, Result<ReservationDto>>
{
  public async ValueTask<Result<ReservationDto>> Handle(ReassignReservationCommand request, CancellationToken cancellationToken)
  {
    var oldReservation = await reservationRepository.FirstOrDefaultAsync(
      new ReservationByIdSpec(request.ReservationId), cancellationToken);
    if (oldReservation == null) return Result.NotFound();

    if (oldReservation.Status != ReservationStatus.Active)
    {
      return Result.Invalid(new ValidationError("ReservationId", "Reservation is not active"));
    }

    if (oldReservation.DriverId == request.NewDriverId)
    {
      return Result.Invalid(new ValidationError("NewDriverId", "Reservation already belongs to this driver"));
    }

    var newDriver = await driverRepository.FirstOrDefaultAsync(
      new DriverByIdSpec(request.NewDriverId), cancellationToken);
    if (newDriver == null) return Result.NotFound();

    var existingDriverLotReservation = await reservationRepository.FirstOrDefaultAsync(
      new ActiveReservationByDriverLotSpec(request.NewDriverId, oldReservation.LotId), cancellationToken);
    if (existingDriverLotReservation != null)
    {
      return Result.Conflict("Driver already has an active reservation in this lot");
    }

    var spaceId = oldReservation.SpaceId;
    var lotId = oldReservation.LotId;
    var previousReservationId = oldReservation.Id;
    var previousDriverId = oldReservation.DriverId;

    Reservation? newReservation = null;

    await unitOfWork.ExecuteInTransactionAsync(async ct =>
    {
      oldReservation.Cancel(request.ActorId);
      await reservationRepository.UpdateAsync(oldReservation, ct);

      newReservation = Reservation.CreateForReassignment(
        spaceId, request.NewDriverId, lotId, previousReservationId, previousDriverId, request.ActorId);
      await reservationRepository.AddAsync(newReservation, ct);
    }, cancellationToken);

    return new ReservationDto(newReservation!.Id, newReservation.SpaceId, newReservation.DriverId,
      newReservation.LotId, newReservation.Status);
  }
}
