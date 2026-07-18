using Ardalis.GuardClauses;

namespace Parkin.Api.Domain.ParkingLotAggregate;

public class ParkingSpace : EntityBase<ParkingSpace, ParkingSpaceId>
{
  // Private constructor for EF Core
  private ParkingSpace() { }

  private ParkingSpace(ParkingSpaceId id, ParkingLotId lotId, string label, SpaceType type)
  {
    Guard.Against.NullOrWhiteSpace(label, nameof(label));

    Id = id;
    LotId = lotId;
    Label = label;
    Type = type;
    Status = SpaceStatus.Active;
  }

  internal static ParkingSpace Create(ParkingLotId lotId, string label, SpaceType type)
    => new(ParkingSpaceId.From(Guid.NewGuid()), lotId, label, type);

  public ParkingLotId LotId { get; private set; }
  public string Label { get; private set; } = string.Empty;
  public SpaceType Type { get; private set; }
  public SpaceStatus Status { get; private set; }

  internal void Rename(string label)
  {
    Guard.Against.NullOrWhiteSpace(label, nameof(label));
    Label = label;
  }

  internal void SetType(SpaceType type) => Type = type;

  internal void Deactivate() => Status = SpaceStatus.Inactive;
}
