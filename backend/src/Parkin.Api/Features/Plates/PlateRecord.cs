using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.Features.Plates;

public record PlateRecord(
  Guid Id,
  Guid DriverId,
  string NormalizedPlateNumber,
  PlateStatus Status);
