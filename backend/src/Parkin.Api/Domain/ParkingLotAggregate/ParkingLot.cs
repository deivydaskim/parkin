using Ardalis.GuardClauses;
using Parkin.Api.Domain.ParkingLotAggregate.Events;

namespace Parkin.Api.Domain.ParkingLotAggregate;

public class ParkingLot : EntityBase<ParkingLot, ParkingLotId>, IAggregateRoot
{
  public const int NameMaxLength = 200;

  private readonly List<ParkingSpace> _spaces = [];

  private ParkingLot() { }

  private ParkingLot(ParkingLotId id, string name, string timezone, string? address,
    AccessMode accessMode, FullBehavior fullBehavior, LotLayout? layout)
  {
    Guard.Against.NullOrWhiteSpace(name, nameof(name));
    Guard.Against.NullOrWhiteSpace(timezone, nameof(timezone));

    Id = id;
    Name = name;
    Timezone = timezone;
    Address = address;
    AccessMode = accessMode;
    FullBehavior = fullBehavior;
    Layout = layout;
    Status = LotStatus.Active;
  }

  public static ParkingLot Create(string name, string timezone, string? address = null,
    AccessMode accessMode = AccessMode.Open, FullBehavior fullBehavior = FullBehavior.Block, Guid? actorId = null,
    LotLayout? layout = null)
  {
    var lot = new ParkingLot(ParkingLotId.From(Guid.CreateVersion7()), name, timezone, address, accessMode,
      fullBehavior, layout);
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

  public IReadOnlyCollection<ParkingSpace> Spaces => _spaces.AsReadOnly();

  public int Capacity => _spaces.Count(s => s.Status == SpaceStatus.Active && s.Type == SpaceType.General);

  public ParkingSpace? FindSpace(ParkingSpaceId spaceId) => _spaces.FirstOrDefault(s => s.Id == spaceId);

  public Result UpdateDetails(string name, string? address, string timezone, LotLayout? layout, Guid? actorId)
  {
    Guard.Against.NullOrWhiteSpace(name, nameof(name));
    Guard.Against.NullOrWhiteSpace(timezone, nameof(timezone));

    var fitResult = CheckSpacesFit(layout, _spaces.Select(s => (s.Id, s.Placement)));
    if (!fitResult.IsSuccess) return fitResult;

    var changes = new Dictionary<string, LotFieldChange>();
    TrackChange(changes, "name", Name, name);
    TrackChange(changes, "address", Address, address);
    TrackChange(changes, "timezone", Timezone, timezone);
    TrackChange(changes, "layout", Layout?.ToString(), layout?.ToString());

    Name = name;
    Address = address;
    Timezone = timezone;
    Layout = layout;

    if (changes.Count > 0)
    {
      RegisterDomainEvent(new LotUpdatedEvent(Id, changes, actorId));
    }

    return Result.Success();
  }

  public void SetAccessMode(AccessMode mode, Guid? actorId)
  {
    if (AccessMode == mode) return;

    RegisterDomainEvent(new LotAccessModeChangedEvent(Id, AccessMode, mode, actorId));
    AccessMode = mode;
  }

  public void SetFullBehavior(FullBehavior behavior, Guid? actorId)
  {
    if (FullBehavior == behavior) return;

    RegisterDomainEvent(new LotFullBehaviorChangedEvent(Id, FullBehavior, behavior, actorId));
    FullBehavior = behavior;
  }

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

  public Result<ParkingSpace> AddSpace(string label, SpaceType type, Guid? actorId,
    string? zone = null, SpacePlacement? placement = null)
  {
    var errors = LabelErrors(label, spaceId: null)
      .Concat(ZoneErrors(zone))
      .Concat(PlacementErrors(placement))
      .ToList();
    if (errors.Count > 0) return Result.Invalid(errors);

    var space = ParkingSpace.Create(Id, label, type);
    space.SetZone(zone);
    space.Place(placement);
    _spaces.Add(space);
    RegisterDomainEvent(new SpaceCreatedEvent(Id, space.Id, actorId));
    return space;
  }

  public Result<ParkingSpace> UpdateSpace(ParkingSpaceId spaceId, string? label, SpaceType? type, Guid? actorId)
    => UpdateSpace(spaceId, new SpaceUpdate(Label: label, Type: type), actorId);

  public Result<ParkingSpace> UpdateSpace(ParkingSpaceId spaceId, SpaceUpdate update, Guid? actorId)
  {
    var space = FindSpace(spaceId);
    if (space is null) return Result.NotFound();

    IEnumerable<ValidationError> labelErrors = update.Label is null ? [] : LabelErrors(update.Label, spaceId);
    var errors = labelErrors
      .Concat(ZoneErrors(update.Zone))
      .Concat(PlacementErrors(update.Placement))
      .ToList();
    if (errors.Count > 0) return Result.Invalid(errors);

    if (update.Label is not null) space.Rename(update.Label);
    if (update.Type.HasValue) space.SetType(update.Type.Value);
    if (update.Zone is not null) space.SetZone(update.Zone);
    if (update.Placement is not null || update.ClearPlacement) space.Place(update.Placement);

    RegisterDomainEvent(new SpaceUpdatedEvent(Id, spaceId, actorId));
    return space;
  }

  public Result<ParkingSpace> DeactivateSpace(ParkingSpaceId spaceId, Guid? actorId)
  {
    var space = FindSpace(spaceId);
    if (space is null) return Result.NotFound();

    space.Deactivate();
    RegisterDomainEvent(new SpaceDeactivatedEvent(Id, spaceId, actorId));
    return space;
  }

  public Result<ParkingSpace> ReactivateSpace(ParkingSpaceId spaceId, Guid? actorId)
  {
    var space = FindSpace(spaceId);
    if (space is null) return Result.NotFound();

    var errors = LabelErrors(space.Label, spaceId).ToList();
    if (errors.Count > 0) return Result.Invalid(errors);

    space.Reactivate();
    RegisterDomainEvent(new SpaceReactivatedEvent(Id, spaceId, actorId));
    return space;
  }

  public Result ApplyLayout(LotLayout? layout, IReadOnlyList<SpaceLayoutChange> changes, Guid? actorId)
  {
    var errors = LayoutChangeErrors(changes).ToList();
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
      var space = FindSpace(change.SpaceId)!;
      space.Place(change.Placement);
      if (change.Zone is not null) space.SetZone(change.Zone);
    }

    var placedCount = changes.Count(c => c.Placement is not null);
    RegisterDomainEvent(new LotLayoutAppliedEvent(Id, placedCount, changes.Count - placedCount, layoutChanged, actorId));
    return Result.Success();
  }

  private IEnumerable<ValidationError> LayoutChangeErrors(IReadOnlyList<SpaceLayoutChange> changes)
  {
    var duplicateIds = changes.GroupBy(c => c.SpaceId).Where(g => g.Count() > 1).Select(g => g.Key);
    foreach (var duplicateId in duplicateIds)
    {
      yield return new ValidationError("Spaces", $"Space {duplicateId.Value} appears more than once in the batch");
    }

    foreach (var change in changes.Where(c => FindSpace(c.SpaceId) is null))
    {
      yield return new ValidationError("Spaces", $"Space {change.SpaceId.Value} does not belong to this lot");
    }

    foreach (var error in changes.SelectMany(c => ZoneErrors(c.Zone)))
    {
      yield return error;
    }
  }

  private IEnumerable<ValidationError> LabelErrors(string label, ParkingSpaceId? spaceId)
  {
    if (string.IsNullOrWhiteSpace(label))
    {
      yield return new ValidationError("Label", "Label is required");
    }
    else if (_spaces.Any(s => s.Label == label && s.Id != spaceId))
    {
      yield return new ValidationError("Label", "A space with this label already exists in this lot");
    }
  }

  private static IEnumerable<ValidationError> ZoneErrors(string? zone)
  {
    var normalized = ParkingSpace.NormalizeZone(zone);
    if (normalized is not null && normalized.Length > ParkingSpace.ZoneMaxLength)
    {
      yield return new ValidationError("Zone", $"Zone must not exceed {ParkingSpace.ZoneMaxLength} characters");
    }
  }

  private IEnumerable<ValidationError> PlacementErrors(SpacePlacement? placement)
  {
    if (placement is not null && Layout is not null && !Layout.Contains(placement))
    {
      yield return PlacementOutsideLayoutError(Layout, placement, "Placement");
    }
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

  private static void TrackChange(Dictionary<string, LotFieldChange> changes, string field, string? from, string? to)
  {
    if (from != to) changes[field] = new LotFieldChange(from, to);
  }
}
