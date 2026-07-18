using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.DriverFeatures;

public record DriverRecord(
  Guid Id,
  string Name,
  string? Contact,
  DriverStatus Status,
  int PlateCount);
