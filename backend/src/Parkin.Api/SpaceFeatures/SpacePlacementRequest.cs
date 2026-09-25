using FluentValidation;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.SpaceFeatures;

public sealed class SpacePlacementRequest
{
  public decimal X { get; init; }
  public decimal Y { get; init; }
  public decimal RotationDegrees { get; init; }
  public int Level { get; init; }
  public decimal Width { get; init; } = SpacePlacement.DefaultWidth;
  public decimal Length { get; init; } = SpacePlacement.DefaultLength;

  public Result<SpacePlacement> ToValue() =>
    SpacePlacement.Create(X, Y, RotationDegrees, Level, Width, Length);
}

public sealed class SpacePlacementRequestValidator : AbstractValidator<SpacePlacementRequest>
{
  public SpacePlacementRequestValidator()
  {
    RuleFor(x => x.X)
      .InclusiveBetween(0, SpacePlacement.MaxCoordinate)
      .WithMessage($"X must be between 0 and {SpacePlacement.MaxCoordinate} metres");

    RuleFor(x => x.Y)
      .InclusiveBetween(0, SpacePlacement.MaxCoordinate)
      .WithMessage($"Y must be between 0 and {SpacePlacement.MaxCoordinate} metres");

    RuleFor(x => x.RotationDegrees)
      .GreaterThanOrEqualTo(0)
      .LessThan(360)
      .WithMessage("Rotation must be in the range [0, 360) degrees");

    RuleFor(x => x.Level)
      .InclusiveBetween(0, SpacePlacement.MaxLevel)
      .WithMessage($"Level must be between 0 and {SpacePlacement.MaxLevel}");

    RuleFor(x => x.Width)
      .GreaterThan(0)
      .LessThanOrEqualTo(SpacePlacement.MaxBaySize)
      .WithMessage($"Width must be greater than 0 and at most {SpacePlacement.MaxBaySize} metres");

    RuleFor(x => x.Length)
      .GreaterThan(0)
      .LessThanOrEqualTo(SpacePlacement.MaxBaySize)
      .WithMessage($"Length must be greater than 0 and at most {SpacePlacement.MaxBaySize} metres");
  }
}
