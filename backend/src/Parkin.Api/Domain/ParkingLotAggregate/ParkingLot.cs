using Ardalis.GuardClauses;
using Parkin.Api.Domain.ParkingLotAggregate.Events;

namespace Parkin.Api.Domain.ParkingLotAggregate;

public class ParkingLot : EntityBase<ParkingLot, ParkingLotId>, IAggregateRoot
{
  // Private constructor for EF Core
  private ParkingLot() { }

  private ParkingLot(ParkingLotId id, string name, string timezone, string? address,
    AccessMode accessMode, FullBehavior fullBehavior)
  {
    Guard.Against.NullOrWhiteSpace(name, nameof(name));
    Guard.Against.NullOrWhiteSpace(timezone, nameof(timezone));

    Id = id;
    Name = name;
    Timezone = timezone;
    Address = address;
    AccessMode = accessMode;
    FullBehavior = fullBehavior;
    Status = LotStatus.Active;
  }

  // Factory method for creating new lots (before persistence)
  public static ParkingLot Create(string name, string timezone, string? address = null,
    AccessMode accessMode = AccessMode.Open, FullBehavior fullBehavior = FullBehavior.Block, Guid? actorId = null)
  {
    var lot = new ParkingLot(ParkingLotId.From(Guid.NewGuid()), name, timezone, address, accessMode, fullBehavior);
    lot.RegisterDomainEvent(new LotCreatedEvent(lot.Id, actorId));
    return lot;
  }

  public string Name { get; private set; } = string.Empty;
  public string? Address { get; private set; }
  public string Timezone { get; private set; } = string.Empty;
  public AccessMode AccessMode { get; private set; }
  public FullBehavior FullBehavior { get; private set; }
  public LotStatus Status { get; private set; }

  private readonly List<ParkingSpace> _spaces = [];
  public IReadOnlyCollection<ParkingSpace> Spaces => _spaces.AsReadOnly();

  public int Capacity => _spaces.Count(s => s.Status == SpaceStatus.Active && s.Type == SpaceType.General);

  public ParkingSpace AddSpace(string label, SpaceType type, Guid? actorId)
  {
    var space = ParkingSpace.Create(Id, label, type);
    _spaces.Add(space);
    RegisterDomainEvent(new SpaceCreatedEvent(Id, space.Id, actorId));
    return space;
  }

  public void UpdateSpace(ParkingSpaceId spaceId, string? label, SpaceType? type, Guid? actorId)
  {
    var space = _spaces.First(s => s.Id == spaceId);

    if (label is not null)
    {
      space.Rename(label);
    }

    if (type.HasValue)
    {
      space.SetType(type.Value);
    }

    RegisterDomainEvent(new SpaceUpdatedEvent(Id, spaceId, actorId));
  }

  public void DeactivateSpace(ParkingSpaceId spaceId, Guid? actorId)
  {
    var space = _spaces.First(s => s.Id == spaceId);
    space.Deactivate();
    RegisterDomainEvent(new SpaceDeactivatedEvent(Id, spaceId, actorId));
  }

  public void ReactivateSpace(ParkingSpaceId spaceId, Guid? actorId)
  {
    var space = _spaces.First(s => s.Id == spaceId);
    space.Reactivate();
    RegisterDomainEvent(new SpaceReactivatedEvent(Id, spaceId, actorId));
  }

  public void UpdateDetails(string name, string? address, string timezone, Guid? actorId)
  {
    Guard.Against.NullOrWhiteSpace(name, nameof(name));
    Guard.Against.NullOrWhiteSpace(timezone, nameof(timezone));

    Name = name;
    Address = address;
    Timezone = timezone;
    RegisterDomainEvent(new LotUpdatedEvent(Id, actorId));
  }

  public void SetAccessMode(AccessMode mode) => AccessMode = mode;

  public void SetFullBehavior(FullBehavior behavior) => FullBehavior = behavior;

  public void Archive(Guid? actorId)
  {
    Status = LotStatus.Archived;
    RegisterDomainEvent(new LotArchivedEvent(Id, actorId));
  }

  public void Restore(Guid? actorId)
  {
    Status = LotStatus.Active;
    RegisterDomainEvent(new LotRestoredEvent(Id, actorId));
  }
}
