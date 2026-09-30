using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;
using Parkin.Api.Domain.Exceptions;
using Parkin.Api.Domain.Interfaces;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Domain.ReservationAggregate.Specifications;

namespace Parkin.Api.Features.Reservations.Create;

public record CreateReservationCommand(
  ParkingSpaceId SpaceId,
  DriverId DriverId,
  Guid? ActorId) : ICommand<Result<ReservationResponse>>;

public class CreateReservationHandler(
  IRepository<ParkingLot> lotRepository,
  IReadRepository<Driver> driverRepository,
  IRepository<Reservation> reservationRepository,
  IUnitOfWork unitOfWork)
  : ICommandHandler<CreateReservationCommand, Result<ReservationResponse>>
{
  public async ValueTask<Result<ReservationResponse>> Handle(CreateReservationCommand request,
    CancellationToken cancellationToken)
  {
    var lot = await lotRepository.FirstOrDefaultAsync(new ParkingLotBySpaceIdSpec(request.SpaceId), cancellationToken);
    if (lot is null) return Result.NotFound();

    if (lot.FindSpace(request.SpaceId)?.Status != SpaceStatus.Active)
    {
      return Result.Invalid(new ValidationError("SpaceId", "Space is not active"));
    }

    if (!await driverRepository.AnyAsync(new DriverByIdSpec(request.DriverId), cancellationToken))
    {
      return Result.NotFound();
    }

    if (await reservationRepository.AnyAsync(new ActiveReservationBySpaceSpec(request.SpaceId), cancellationToken))
    {
      return Result.Conflict(ReservationConflicts.SpaceAlreadyReserved);
    }

    if (await reservationRepository.AnyAsync(
          new ActiveReservationByDriverLotSpec(request.DriverId, lot.Id), cancellationToken))
    {
      return Result.Conflict(ReservationConflicts.DriverAlreadyReservedInLot);
    }

    var markReserved = lot.UpdateSpace(request.SpaceId, new SpaceUpdate(Type: SpaceType.Reserved), request.ActorId);
    if (!markReserved.IsSuccess) return Result.Invalid(markReserved.ValidationErrors);

    var reservation = Reservation.Create(request.SpaceId, request.DriverId, lot.Id, request.ActorId);
    try
    {
      await unitOfWork.ExecuteInTransactionAsync(async ct =>
      {
        await reservationRepository.AddAsync(reservation, ct);
        await lotRepository.UpdateAsync(lot, ct);
      }, cancellationToken);
    }
    catch (UniqueConstraintViolationException exception)
      when (ReservationConflicts.MessageFor(exception) is { } message)
    {
      return Result.Conflict(message);
    }

    return ReservationResponse.From(reservation);
  }
}
