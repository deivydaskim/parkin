using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.Features.Plates;

public record PlateResponse(
  Guid Id,
  Guid DriverId,
  string NormalizedPlateNumber,
  PlateStatus Status)
{
  public static PlateResponse From(Plate plate)
    => new(plate.Id.Value, plate.DriverId.Value, plate.NormalizedPlateNumber, plate.Status);
}
