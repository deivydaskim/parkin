namespace Parkin.Api.Domain.ParkingLotAggregate;

public sealed class LotLayout : IEquatable<LotLayout>
{
  public const decimal MaxDimension = 100_000m;
  public const int MaxLevelCount = 200;

  private LotLayout() { }

  private LotLayout(decimal widthMeters, decimal lengthMeters, int levelCount)
  {
    WidthMeters = widthMeters;
    LengthMeters = lengthMeters;
    LevelCount = levelCount;
  }

  public decimal WidthMeters { get; private set; }
  public decimal LengthMeters { get; private set; }
  public int LevelCount { get; private set; }

  public static Result<LotLayout> Create(decimal widthMeters, decimal lengthMeters, int levelCount = 1)
  {
    var errors = new List<ValidationError>();

    if (widthMeters <= 0 || widthMeters > MaxDimension)
      errors.Add(new ValidationError(nameof(WidthMeters), $"Width must be greater than 0 and at most {MaxDimension} metres"));
    if (lengthMeters <= 0 || lengthMeters > MaxDimension)
      errors.Add(new ValidationError(nameof(LengthMeters), $"Length must be greater than 0 and at most {MaxDimension} metres"));
    if (levelCount < 1 || levelCount > MaxLevelCount)
      errors.Add(new ValidationError(nameof(LevelCount), $"Level count must be between 1 and {MaxLevelCount}"));

    if (errors.Count > 0) return Result.Invalid(errors);

    return new LotLayout(Math.Round(widthMeters, 2), Math.Round(lengthMeters, 2), levelCount);
  }

  public bool Contains(SpacePlacement placement) =>
    placement.X <= WidthMeters && placement.Y <= LengthMeters && placement.Level < LevelCount;

  public bool Equals(LotLayout? other) =>
    other is not null && WidthMeters == other.WidthMeters && LengthMeters == other.LengthMeters
    && LevelCount == other.LevelCount;

  public override bool Equals(object? obj) => Equals(obj as LotLayout);

  public override int GetHashCode() => HashCode.Combine(WidthMeters, LengthMeters, LevelCount);
}
