namespace Parkin.Api.Domain.DriverAggregate.Specifications;

public class DriverByPlateIdSpec : Specification<Driver>
{
  public DriverByPlateIdSpec(PlateId plateId) =>
    Query
        .Include(driver => driver.Plates)
        .Where(driver => driver.Plates.Any(p => p.Id == plateId));
}
