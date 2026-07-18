namespace Parkin.Api.Domain.DriverAggregate.Specifications;

public class DriverByIdSpec : Specification<Driver>
{
  public DriverByIdSpec(DriverId driverId) =>
    Query
        .Where(driver => driver.Id == driverId)
        .Include(driver => driver.Plates);
}
