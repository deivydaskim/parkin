using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Domain.ReservationAggregate.Specifications;

namespace Parkin.Api.Features.Reservations.GetActiveBySpace;

public record GetActiveReservationBySpaceQuery(ParkingSpaceId SpaceId) : IQuery<ReservationResponse?>;

public class GetActiveReservationBySpaceHandler(IReadRepository<Reservation> repository)
  : IQueryHandler<GetActiveReservationBySpaceQuery, ReservationResponse?>
{
  public async ValueTask<ReservationResponse?> Handle(GetActiveReservationBySpaceQuery request,
    CancellationToken cancellationToken)
  {
    var reservation = await repository.FirstOrDefaultAsync(
      new ActiveReservationBySpaceSpec(request.SpaceId), cancellationToken);

    return reservation is null ? null : ReservationResponse.From(reservation);
  }
}
