using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Domain.ReservationAggregate.Specifications;

namespace Parkin.Api.ReservationFeatures.Cancel;

public record CancelReservationCommand(ReservationId ReservationId, Guid? ActorId) : ICommand<Result<ReservationDto>>;

public class CancelReservationHandler(IRepository<Reservation> repository)
  : ICommandHandler<CancelReservationCommand, Result<ReservationDto>>
{
  public async ValueTask<Result<ReservationDto>> Handle(CancelReservationCommand request, CancellationToken cancellationToken)
  {
    var reservation = await repository.FirstOrDefaultAsync(new ReservationByIdSpec(request.ReservationId), cancellationToken);
    if (reservation == null) return Result.NotFound();

    reservation.Cancel(request.ActorId);
    await repository.UpdateAsync(reservation, cancellationToken);

    return new ReservationDto(reservation.Id, reservation.SpaceId, reservation.DriverId, reservation.LotId, reservation.Status);
  }
}
