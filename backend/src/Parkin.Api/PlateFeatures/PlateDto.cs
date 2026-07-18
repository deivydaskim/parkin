using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.PlateFeatures;

public record PlateDto(
  PlateId Id,
  DriverId DriverId,
  string NormalizedPlateNumber,
  PlateStatus Status);
