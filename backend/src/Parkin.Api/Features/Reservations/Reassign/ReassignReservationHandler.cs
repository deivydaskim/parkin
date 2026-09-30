using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;
using Parkin.Api.Domain.Exceptions;
using Parkin.Api.Domain.Interfaces;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Domain.ReservationAggregate.Specifications;

namespace Parkin.Api.Features.Reservations.Reassign;

public record ReassignReservationCommand(
  ReservationId ReservationId,
  DriverId NewDriverId,
  Guid? ActorId) : ICommand<Result<ReservationResponse>>;

public class ReassignReservationHandler(
  IRepository<Reservation> reservationRepository,
  IReadRepository<Driver> driverRepository,
  IUnitOfWork unitOfWork)
  : ICommandHandler<ReassignReservationCommand, Result<ReservationResponse>>
{
  public async ValueTask<Result<ReservationResponse>> Handle(ReassignReservationCommand request,
    CancellationToken cancellationToken)
  {
    var oldReservation = await reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken);
    if (oldReservation is null) return Result.NotFound();

    var reassignment = oldReservation.ReassignTo(request.NewDriverId, request.ActorId);
    if (!reassignment.IsSuccess) return reassignment.Map(ReservationResponse.From);

    if (!await driverRepository.AnyAsync(new DriverByIdSpec(request.NewDriverId), cancellationToken))
    {
      return Result.NotFound();
    }

    if (await reservationRepository.AnyAsync(
          new ActiveReservationByDriverLotSpec(request.NewDriverId, oldReservation.LotId), cancellationToken))
    {
      return Result.Conflict(ReservationConflicts.DriverAlreadyReservedInLot);
    }

    var newReservation = reassignment.Value;
    try
    {
      await unitOfWork.ExecuteInTransactionAsync(async ct =>
      {
        await reservationRepository.UpdateAsync(oldReservation, ct);
        await reservationRepository.AddAsync(newReservation, ct);
      }, cancellationToken);
    }
    catch (UniqueConstraintViolationException exception)
      when (ReservationConflicts.MessageFor(exception) is { } message)
    {
      return Result.Conflict(message);
    }

    return ReservationResponse.From(newReservation);
  }
}
