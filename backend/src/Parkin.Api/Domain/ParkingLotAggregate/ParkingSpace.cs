using Ardalis.GuardClauses;

namespace Parkin.Api.Domain.ParkingLotAggregate;

public class ParkingSpace : EntityBase<ParkingSpace, ParkingSpaceId>
{
  public const int ZoneMaxLength = 50;

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
  public string? Zone { get; private set; }
  public SpacePlacement? Placement { get; private set; }

  public static string? NormalizeZone(string? zone) =>
    string.IsNullOrWhiteSpace(zone) ? null : zone.Trim();

  internal void Rename(string label)
  {
    Guard.Against.NullOrWhiteSpace(label, nameof(label));
    Label = label;
  }

  internal void SetType(SpaceType type) => Type = type;

  internal void SetZone(string? zone) => Zone = NormalizeZone(zone);

  internal void Place(SpacePlacement? placement) => Placement = placement;

  internal void Deactivate() => Status = SpaceStatus.Inactive;

  internal void Reactivate() => Status = SpaceStatus.Active;
}
