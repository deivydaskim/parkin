using Ardalis.GuardClauses;
using Parkin.Api.Domain.DriverAggregate.Events;

namespace Parkin.Api.Domain.DriverAggregate;

public class Driver : EntityBase<Driver, DriverId>, IAggregateRoot
{
  private Driver() { }

  private Driver(DriverId id, string name, string? contact)
  {
    Guard.Against.NullOrWhiteSpace(name, nameof(name));

    Id = id;
    Name = name;
    Contact = contact;
    Status = DriverStatus.Active;
  }

  public static Driver Create(string name, string? contact, Guid? actorId)
  {
    var driver = new Driver(DriverId.From(Guid.CreateVersion7()), name, contact);
    driver.RegisterDomainEvent(new DriverCreatedEvent(driver.Id, actorId));
    return driver;
  }

  public string Name { get; private set; } = string.Empty;
  public string? Contact { get; private set; }
  public DriverStatus Status { get; private set; }

  private readonly List<Plate> _plates = [];
  public IReadOnlyCollection<Plate> Plates => _plates.AsReadOnly();

  public void UpdateDetails(string name, string? contact, Guid? actorId)
  {
    Guard.Against.NullOrWhiteSpace(name, nameof(name));

    Name = name;
    Contact = contact;
    RegisterDomainEvent(new DriverUpdatedEvent(Id, actorId));
  }

  public void Archive(Guid? actorId)
  {
    Status = DriverStatus.Archived;
    RegisterDomainEvent(new DriverArchivedEvent(Id, actorId));
  }

  public void Restore(Guid? actorId)
  {
    Status = DriverStatus.Active;
    RegisterDomainEvent(new DriverRestoredEvent(Id, actorId));
  }

  public Plate AddPlate(string rawPlate, Guid? actorId)
  {
    var plate = Plate.Create(Id, PlateNormalizer.Normalize(rawPlate));
    _plates.Add(plate);
    RegisterDomainEvent(new PlateAddedEvent(Id, plate.Id, actorId));
    return plate;
  }

  public Result<Plate> DeactivatePlate(PlateId plateId, Guid? actorId)
  {
    var plate = FindPlate(plateId);
    if (plate is null) return Result.NotFound();

    plate.Deactivate();
    RegisterDomainEvent(new PlateDeactivatedEvent(Id, plateId, actorId));
    return plate;
  }

  public Result<Plate> ReactivatePlate(PlateId plateId, Guid? actorId)
  {
    var plate = FindPlate(plateId);
    if (plate is null) return Result.NotFound();

    plate.Reactivate();
    RegisterDomainEvent(new PlateReactivatedEvent(Id, plateId, actorId));
    return plate;
  }

  public Result<Plate> TransferPlate(PlateId plateId, Driver targetDriver, Guid? actorId)
  {
    var plate = FindPlate(plateId);
    if (plate is null) return Result.NotFound();

    if (targetDriver.Id == Id)
    {
      return Result.Invalid(new ValidationError("TargetDriverId", "Plate already belongs to this driver"));
    }

    _plates.Remove(plate);
    plate.ReassignTo(targetDriver.Id);
    targetDriver._plates.Add(plate);
    targetDriver.RegisterDomainEvent(new PlateReassignedEvent(plate.Id, Id, targetDriver.Id, actorId));
    return plate;
  }

  private Plate? FindPlate(PlateId plateId) => _plates.FirstOrDefault(plate => plate.Id == plateId);
}
