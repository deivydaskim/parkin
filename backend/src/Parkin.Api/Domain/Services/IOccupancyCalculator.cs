namespace Parkin.Api.Domain.Services;

public interface IOccupancyCalculator
{
  OccupancyResult Calculate(OccupancyContext context);
}
