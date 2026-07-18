using Ardalis.GuardClauses;
using Parkin.Api.Domain.DriverAggregate.Events;

namespace Parkin.Api.Domain.DriverAggregate;

public class Driver : EntityBase<Driver, DriverId>, IAggregateRoot
{
  // Private constructor for EF Core
  private Driver() { }

  private Driver(DriverId id, string name, string? contact)
  {
    Guard.Against.NullOrWhiteSpace(name, nameof(name));

    Id = id;
    Name = name;
    Contact = contact;
    Status = DriverStatus.Active;
  }

  // Factory method for creating new drivers (before persistence)
  public static Driver Create(string name, string? contact, Guid? actorId)
  {
    var driver = new Driver(DriverId.From(Guid.NewGuid()), name, contact);
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

  public Plate AddPlate(string rawPlate, Guid? actorId)
  {
    var normalized = PlateNormalizer.Normalize(rawPlate);
    var plate = Plate.Create(Id, normalized);
    _plates.Add(plate);
    RegisterDomainEvent(new PlateAddedEvent(Id, plate.Id, actorId));
    return plate;
  }

  internal Plate RemovePlateForReassignment(PlateId plateId)
  {
    var plate = _plates.First(p => p.Id == plateId);
    _plates.Remove(plate);
    return plate;
  }

  internal void ReceivePlate(Plate plate, DriverId fromDriverId, Guid? actorId)
  {
    plate.ReassignTo(Id);
    _plates.Add(plate);
    RegisterDomainEvent(new PlateReassignedEvent(plate.Id, fromDriverId, Id, actorId));
  }

  public void DeactivatePlate(PlateId plateId, Guid? actorId)
  {
    var plate = _plates.First(p => p.Id == plateId);
    plate.Deactivate();
    RegisterDomainEvent(new PlateDeactivatedEvent(Id, plateId, actorId));
  }

  public void Archive(Guid? actorId)
  {
    Status = DriverStatus.Archived;
    RegisterDomainEvent(new DriverArchivedEvent(Id, actorId));
  }
}
