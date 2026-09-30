using Parkin.Api.Domain.ReservationAggregate;

namespace Parkin.Api.Features.Reservations.Cancel;

public record CancelReservationCommand(ReservationId ReservationId, Guid? ActorId)
  : ICommand<Result<ReservationResponse>>;

public class CancelReservationHandler(IRepository<Reservation> repository)
  : ICommandHandler<CancelReservationCommand, Result<ReservationResponse>>
{
  public async ValueTask<Result<ReservationResponse>> Handle(CancelReservationCommand request,
    CancellationToken cancellationToken)
  {
    var reservation = await repository.GetByIdAsync(request.ReservationId, cancellationToken);
    if (reservation is null) return Result.NotFound();

    var result = reservation.Cancel(request.ActorId);
    if (!result.IsSuccess) return result;

    await repository.UpdateAsync(reservation, cancellationToken);
    return ReservationResponse.From(reservation);
  }
}
