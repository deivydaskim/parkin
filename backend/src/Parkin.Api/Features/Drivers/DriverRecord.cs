using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.Features.Drivers;

public record DriverRecord(
  Guid Id,
  string Name,
  string? Contact,
  DriverStatus Status,
  int PlateCount);
