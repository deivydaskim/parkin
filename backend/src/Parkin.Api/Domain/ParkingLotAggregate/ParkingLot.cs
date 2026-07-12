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
    AccessMode accessMode = AccessMode.Open, FullBehavior fullBehavior = FullBehavior.Block)
    => new(ParkingLotId.From(Guid.NewGuid()), name, timezone, address, accessMode, fullBehavior);

  public string Name { get; private set; } = string.Empty;
  public string? Address { get; private set; }
  public string Timezone { get; private set; } = string.Empty;
  public AccessMode AccessMode { get; private set; }
  public FullBehavior FullBehavior { get; private set; }
  public LotStatus Status { get; private set; }

  public void UpdateDetails(string name, string? address, string timezone)
  {
    Guard.Against.NullOrWhiteSpace(name, nameof(name));
    Guard.Against.NullOrWhiteSpace(timezone, nameof(timezone));

    Name = name;
    Address = address;
    Timezone = timezone;
  }

  public void SetAccessMode(AccessMode mode) => AccessMode = mode;

  public void SetFullBehavior(FullBehavior behavior) => FullBehavior = behavior;

  public void Archive()
  {
    Status = LotStatus.Archived;
    RegisterDomainEvent(new LotArchivedEvent(Id));
  }
}
