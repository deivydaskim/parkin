using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.LotLayoutFeatures;
using Parkin.Api.LotLayoutFeatures.Get;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class LotLayoutQueryService(AppDbContext db) : ILotLayoutQueryService
{
  public Task<LotLayoutViewDto?> GetAsync(ParkingLotId lotId, CancellationToken cancellationToken) =>
    db.ParkingLots
      .AsNoTracking()
      .Where(lot => lot.Id == lotId)
      .Select(lot => new LotLayoutViewDto(
        lot.Id,
        lot.Name,
        lot.Status,
        lot.Layout,
        lot.Spaces
          .OrderBy(space => space.Label)
          .Select(space => new LotLayoutSpaceDto(
            space.Id,
            space.Label,
            space.Type,
            space.Status,
            space.Zone,
            space.Placement,
            db.Reservations
              .Where(reservation => reservation.SpaceId == space.Id && reservation.Status == ReservationStatus.Active)
              .Join(db.Drivers,
                reservation => reservation.DriverId,
                driver => driver.Id,
                (reservation, driver) => new LotLayoutReservationDto(reservation.Id, driver.Id, driver.Name))
              .FirstOrDefault()))
          .ToList()))
      .FirstOrDefaultAsync(cancellationToken);
}
