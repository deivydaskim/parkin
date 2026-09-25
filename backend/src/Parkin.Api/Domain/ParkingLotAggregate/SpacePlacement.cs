namespace Parkin.Api.Domain.ParkingLotAggregate;

public sealed class SpacePlacement : IEquatable<SpacePlacement>
{
  public const decimal DefaultWidth = 2.5m;
  public const decimal DefaultLength = 5.0m;
  public const decimal MaxCoordinate = 100_000m;
  public const decimal MaxBaySize = 50m;
  public const int MaxLevel = 200;

  private SpacePlacement() { }

  private SpacePlacement(decimal x, decimal y, decimal rotationDegrees, int level, decimal width, decimal length)
  {
    X = x;
    Y = y;
    RotationDegrees = rotationDegrees;
    Level = level;
    Width = width;
    Length = length;
  }

  public decimal X { get; private set; }
  public decimal Y { get; private set; }
  public decimal RotationDegrees { get; private set; }
  public int Level { get; private set; }
  public decimal Width { get; private set; }
  public decimal Length { get; private set; }

  public static Result<SpacePlacement> Create(decimal x, decimal y, decimal rotationDegrees = 0m, int level = 0,
    decimal width = DefaultWidth, decimal length = DefaultLength)
  {
    var errors = new List<ValidationError>();

    if (x < 0 || x > MaxCoordinate)
      errors.Add(new ValidationError(nameof(X), $"X must be between 0 and {MaxCoordinate} metres"));
    if (y < 0 || y > MaxCoordinate)
      errors.Add(new ValidationError(nameof(Y), $"Y must be between 0 and {MaxCoordinate} metres"));
    if (rotationDegrees < 0 || rotationDegrees >= 360)
      errors.Add(new ValidationError(nameof(RotationDegrees), "Rotation must be in the range [0, 360) degrees"));
    if (level < 0 || level > MaxLevel)
      errors.Add(new ValidationError(nameof(Level), $"Level must be between 0 and {MaxLevel}"));
    if (width <= 0 || width > MaxBaySize)
      errors.Add(new ValidationError(nameof(Width), $"Width must be greater than 0 and at most {MaxBaySize} metres"));
    if (length <= 0 || length > MaxBaySize)
      errors.Add(new ValidationError(nameof(Length), $"Length must be greater than 0 and at most {MaxBaySize} metres"));

    if (errors.Count > 0) return Result.Invalid(errors);

    return new SpacePlacement(
      Math.Round(x, 2), Math.Round(y, 2), Math.Round(rotationDegrees, 2), level,
      Math.Round(width, 2), Math.Round(length, 2));
  }

  public bool Equals(SpacePlacement? other) =>
    other is not null && X == other.X && Y == other.Y && RotationDegrees == other.RotationDegrees
    && Level == other.Level && Width == other.Width && Length == other.Length;

  public override bool Equals(object? obj) => Equals(obj as SpacePlacement);

  public override int GetHashCode() => HashCode.Combine(X, Y, RotationDegrees, Level, Width, Length);
}
