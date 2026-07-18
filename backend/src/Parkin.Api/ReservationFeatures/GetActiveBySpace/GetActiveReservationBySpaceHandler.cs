using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Domain.ReservationAggregate.Specifications;

namespace Parkin.Api.ReservationFeatures.GetActiveBySpace;

public record GetActiveReservationBySpaceQuery(ParkingSpaceId SpaceId) : IQuery<Result<ReservationDto?>>;

public class GetActiveReservationBySpaceHandler(IReadRepository<Reservation> repository)
  : IQueryHandler<GetActiveReservationBySpaceQuery, Result<ReservationDto?>>
{
  public async ValueTask<Result<ReservationDto?>> Handle(GetActiveReservationBySpaceQuery request, CancellationToken cancellationToken)
  {
    var reservation = await repository.FirstOrDefaultAsync(new ActiveReservationBySpaceSpec(request.SpaceId), cancellationToken);
    if (reservation == null) return Result.Success<ReservationDto?>(null);

    return Result.Success<ReservationDto?>(
      new ReservationDto(reservation.Id, reservation.SpaceId, reservation.DriverId, reservation.LotId, reservation.Status));
  }
}
