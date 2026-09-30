using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.Features.Drivers;

public record DriverResponse(
  Guid Id,
  string Name,
  string? Contact,
  DriverStatus Status,
  int PlateCount)
{
  public static DriverResponse From(Driver driver)
    => new(driver.Id.Value, driver.Name, driver.Contact, driver.Status, driver.Plates.Count);
}
