namespace Parkin.Api.Domain.DriverAggregate;

public class Plate : EntityBase<Plate, PlateId>
{
  // Private constructor for EF Core
  private Plate() { }

  private Plate(PlateId id, DriverId driverId, string normalizedPlateNumber)
  {
    Id = id;
    DriverId = driverId;
    NormalizedPlateNumber = normalizedPlateNumber;
    Status = PlateStatus.Active;
  }

  internal static Plate Create(DriverId driverId, string normalizedPlateNumber)
    => new(PlateId.From(Guid.NewGuid()), driverId, normalizedPlateNumber);

  public DriverId DriverId { get; private set; }
  public string NormalizedPlateNumber { get; private set; } = string.Empty;
  public PlateStatus Status { get; private set; }

  internal void ReassignTo(DriverId newDriverId) => DriverId = newDriverId;

  internal void Deactivate() => Status = PlateStatus.Inactive;

  internal void Reactivate() => Status = PlateStatus.Active;
}
