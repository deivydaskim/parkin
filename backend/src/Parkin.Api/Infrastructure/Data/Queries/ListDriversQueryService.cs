using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Features.Drivers;
using Parkin.Api.Features.Drivers.List;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ListDriversQueryService(AppDbContext db) : IListDriversQueryService
{
  public Task<PagedResult<DriverResponse>> ListAsync(int page, int perPage, DriverStatusFilter? status,
    string? search, CancellationToken cancellationToken)
    => ApplySearch(ApplyStatus(db.Drivers.AsNoTracking(), status), search)
      .OrderBy(driver => driver.Name)
      .Select(driver => new DriverResponse(
        driver.Id.Value, driver.Name, driver.Contact, driver.Status, driver.Plates.Count))
      .ToPagedResultAsync(page, perPage, cancellationToken);

  private static IQueryable<Driver> ApplyStatus(IQueryable<Driver> query, DriverStatusFilter? status)
    => status switch
    {
      DriverStatusFilter.All => query,
      DriverStatusFilter.Archived => query.Where(driver => driver.Status == DriverStatus.Archived),
      _ => query.Where(driver => driver.Status == DriverStatus.Active),
    };

  private static IQueryable<Driver> ApplySearch(IQueryable<Driver> query, string? search)
  {
    if (string.IsNullOrWhiteSpace(search)) return query;

    var pattern = LikePattern.Containing(search.Trim());
    var platePattern = LikePattern.Containing(PlateNormalizer.Normalize(search));
    return query.Where(driver => EF.Functions.ILike(driver.Name, pattern) ||
      (driver.Contact != null && EF.Functions.ILike(driver.Contact, pattern)) ||
      driver.Plates.Any(plate => EF.Functions.ILike(plate.NormalizedPlateNumber, platePattern)));
  }
}
