using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.PlateFeatures;

public record PlateRecord(
  Guid Id,
  Guid DriverId,
  string NormalizedPlateNumber,
  PlateStatus Status);
