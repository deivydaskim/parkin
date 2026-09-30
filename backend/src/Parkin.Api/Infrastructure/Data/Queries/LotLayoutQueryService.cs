using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Features.LotLayouts;
using Parkin.Api.Features.LotLayouts.Get;
using Parkin.Api.Features.Lots;
using Parkin.Api.Features.Spaces;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class LotLayoutQueryService(AppDbContext db) : ILotLayoutQueryService
{
  public async Task<LotLayoutViewResponse?> GetAsync(ParkingLotId lotId, CancellationToken cancellationToken)
  {
    var row = await db.ParkingLots
      .AsNoTracking()
      .Where(lot => lot.Id == lotId)
      .Select(lot => new
      {
        lot.Id,
        lot.Name,
        lot.Status,
        lot.Layout,
        Spaces = lot.Spaces
          .OrderBy(space => space.Label)
          .Select(space => new
          {
            space.Id,
            space.Label,
            space.Type,
            space.Status,
            space.Zone,
            space.Placement,
            Reservation = db.Reservations
              .Where(reservation => reservation.SpaceId == space.Id && reservation.Status == ReservationStatus.Active)
              .Join(db.Drivers,
                reservation => reservation.DriverId,
                driver => driver.Id,
                (reservation, driver) => new { reservation.Id, DriverId = driver.Id, DriverName = driver.Name })
              .FirstOrDefault(),
          })
          .ToList(),
      })
      .FirstOrDefaultAsync(cancellationToken);

    if (row is null) return null;

    var spaces = row.Spaces
      .Select(space => new LotLayoutSpaceResponse(
        space.Id.Value,
        space.Label,
        space.Type,
        space.Status,
        space.Zone,
        SpacePlacementResponse.From(space.Placement),
        space.Reservation is null
          ? null
          : new LotLayoutReservationResponse(
            space.Reservation.Id.Value, space.Reservation.DriverId.Value, space.Reservation.DriverName)))
      .ToList();

    return new LotLayoutViewResponse(
      new LotLayoutLotResponse(row.Id.Value, row.Name, row.Status, LotLayoutResponse.From(row.Layout)),
      spaces);
  }
}
