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
    AccessMode accessMode = AccessMode.Open, FullBehavior fullBehavior = FullBehavior.Block, Guid? actorId = null,
    LotLayout? layout = null)
  {
    var lot = new ParkingLot(ParkingLotId.From(Guid.NewGuid()), name, timezone, address, accessMode, fullBehavior);
    lot.Layout = layout;
    lot.RegisterDomainEvent(new LotCreatedEvent(lot.Id, actorId));
    return lot;
  }

  public string Name { get; private set; } = string.Empty;
  public string? Address { get; private set; }
  public string Timezone { get; private set; } = string.Empty;
  public AccessMode AccessMode { get; private set; }
  public FullBehavior FullBehavior { get; private set; }
  public LotStatus Status { get; private set; }
  public LotLayout? Layout { get; private set; }

  private readonly List<ParkingSpace> _spaces = [];
  public IReadOnlyCollection<ParkingSpace> Spaces => _spaces.AsReadOnly();

  public int Capacity => _spaces.Count(s => s.Status == SpaceStatus.Active && s.Type == SpaceType.General);

  public ParkingSpace AddSpace(string label, SpaceType type, Guid? actorId,
    string? zone = null, SpacePlacement? placement = null)
  {
    if (placement is not null && !CheckPlacement(placement).IsSuccess)
    {
      throw new ArgumentException("Placement does not fit the lot layout", nameof(placement));
    }

    var space = ParkingSpace.Create(Id, label, type);
    space.SetZone(zone);
    space.Place(placement);
    _spaces.Add(space);
    RegisterDomainEvent(new SpaceCreatedEvent(Id, space.Id, actorId));
    return space;
  }

  public void UpdateSpace(ParkingSpaceId spaceId, string? label, SpaceType? type, Guid? actorId)
    => UpdateSpace(spaceId, new SpaceUpdate(Label: label, Type: type), actorId);

  public Result UpdateSpace(ParkingSpaceId spaceId, SpaceUpdate update, Guid? actorId)
  {
    var space = _spaces.FirstOrDefault(s => s.Id == spaceId);
    if (space is null) return Result.NotFound();

    var zoneResult = CheckZone(update.Zone);
    if (!zoneResult.IsSuccess) return zoneResult;

    if (update.Placement is not null)
    {
      var placementResult = CheckPlacement(update.Placement);
      if (!placementResult.IsSuccess) return placementResult;
    }

    if (update.Label is not null)
    {
      space.Rename(update.Label);
    }

    if (update.Type.HasValue)
    {
      space.SetType(update.Type.Value);
    }

    if (update.Zone is not null)
    {
      space.SetZone(update.Zone);
    }

    if (update.Placement is not null)
    {
      space.Place(update.Placement);
    }
    else if (update.ClearPlacement)
    {
      space.Place(null);
    }

    RegisterDomainEvent(new SpaceUpdatedEvent(Id, spaceId, actorId));
    return Result.Success();
  }

  public Result PlaceSpace(ParkingSpaceId spaceId, SpacePlacement? placement, string? zone, Guid? actorId)
    => UpdateSpace(spaceId, new SpaceUpdate(Zone: zone, Placement: placement, ClearPlacement: placement is null), actorId);

  public Result SetLayout(LotLayout? layout)
  {
    var fitResult = CheckSpacesFit(layout, _spaces.Select(s => (s.Id, s.Placement)));
    if (!fitResult.IsSuccess) return fitResult;

    Layout = layout;
    return Result.Success();
  }

  public Result ApplyLayout(LotLayout? layout, IReadOnlyList<SpaceLayoutChange> changes, Guid? actorId)
  {
    var errors = new List<ValidationError>();

    var duplicateIds = changes.GroupBy(c => c.SpaceId).Where(g => g.Count() > 1).Select(g => g.Key);
    foreach (var duplicateId in duplicateIds)
    {
      errors.Add(new ValidationError("Spaces", $"Space {duplicateId.Value} appears more than once in the batch"));
    }

    var knownIds = _spaces.Select(s => s.Id).ToHashSet();
    foreach (var change in changes.Where(c => !knownIds.Contains(c.SpaceId)))
    {
      errors.Add(new ValidationError("Spaces", $"Space {change.SpaceId.Value} does not belong to this lot"));
    }

    foreach (var change in changes)
    {
      var zoneResult = CheckZone(change.Zone);
      if (!zoneResult.IsSuccess) errors.AddRange(zoneResult.ValidationErrors);
    }

    if (errors.Count > 0) return Result.Invalid(errors);

    var changesById = changes.ToDictionary(c => c.SpaceId);
    var finalPlacements = _spaces.Select(s =>
      (s.Id, changesById.TryGetValue(s.Id, out var change) ? change.Placement : s.Placement));
    var fitResult = CheckSpacesFit(layout, finalPlacements);
    if (!fitResult.IsSuccess) return fitResult;

    var layoutChanged = !Equals(Layout, layout);
    Layout = layout;

    foreach (var change in changes)
    {
      var space = _spaces.First(s => s.Id == change.SpaceId);
      space.Place(change.Placement);
      if (change.Zone is not null)
      {
        space.SetZone(change.Zone);
      }
    }

    var placedCount = changes.Count(c => c.Placement is not null);
    RegisterDomainEvent(new LotLayoutAppliedEvent(Id, placedCount, changes.Count - placedCount, layoutChanged, actorId));
    return Result.Success();
  }

  public Result CheckPlacement(SpacePlacement placement)
  {
    if (Layout is null || Layout.Contains(placement)) return Result.Success();

    return Result.Invalid(PlacementOutsideLayoutError(Layout, placement, "Placement"));
  }

  private static Result CheckSpacesFit(LotLayout? layout,
    IEnumerable<(ParkingSpaceId SpaceId, SpacePlacement? Placement)> placements)
  {
    if (layout is null) return Result.Success();

    var errors = placements
      .Where(p => p.Placement is not null && !layout.Contains(p.Placement))
      .Select(p => PlacementOutsideLayoutError(layout, p.Placement!, $"Spaces[{p.SpaceId.Value}]"))
      .ToList();

    return errors.Count > 0 ? Result.Invalid(errors) : Result.Success();
  }

  private static ValidationError PlacementOutsideLayoutError(LotLayout layout, SpacePlacement placement, string identifier)
  {
    if (placement.Level >= layout.LevelCount)
    {
      return new ValidationError(identifier,
        $"Level {placement.Level} does not exist; the lot has {layout.LevelCount} level(s)");
    }

    return new ValidationError(identifier,
      $"Position ({placement.X}, {placement.Y}) is outside the lot footprint {layout.WidthMeters} x {layout.LengthMeters} m");
  }

  private static Result CheckZone(string? zone)
  {
    var normalized = ParkingSpace.NormalizeZone(zone);
    if (normalized is not null && normalized.Length > ParkingSpace.ZoneMaxLength)
    {
      return Result.Invalid(new ValidationError("Zone", $"Zone must not exceed {ParkingSpace.ZoneMaxLength} characters"));
    }

    return Result.Success();
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
