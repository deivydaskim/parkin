using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.Features.Drivers;

public record DriverDto(
  DriverId Id,
  string Name,
  string? Contact,
  DriverStatus Status,
  int PlateCount);
