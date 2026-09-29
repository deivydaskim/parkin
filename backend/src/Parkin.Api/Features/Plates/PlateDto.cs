using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.Features.Plates;

public record PlateDto(
  PlateId Id,
  DriverId DriverId,
  string NormalizedPlateNumber,
  PlateStatus Status);
