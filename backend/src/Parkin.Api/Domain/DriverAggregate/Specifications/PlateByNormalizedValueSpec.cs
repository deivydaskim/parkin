namespace Parkin.Api.Domain.DriverAggregate.Specifications;

public class PlateByNormalizedValueSpec : Specification<Driver>
{
  public PlateByNormalizedValueSpec(string normalizedPlateNumber) =>
    Query
        .Include(driver => driver.Plates)
        .Where(driver => driver.Plates.Any(p => p.NormalizedPlateNumber == normalizedPlateNumber));
}
