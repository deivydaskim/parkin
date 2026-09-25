using FluentValidation;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.LotFeatures;

public sealed class LotLayoutRequest
{
  public decimal WidthMeters { get; init; }
  public decimal LengthMeters { get; init; }
  public int LevelCount { get; init; } = 1;

  public Result<LotLayout> ToValue() => LotLayout.Create(WidthMeters, LengthMeters, LevelCount);
}

public sealed class LotLayoutRequestValidator : AbstractValidator<LotLayoutRequest>
{
  public LotLayoutRequestValidator()
  {
    RuleFor(x => x.WidthMeters)
      .GreaterThan(0)
      .LessThanOrEqualTo(LotLayout.MaxDimension)
      .WithMessage($"Width must be greater than 0 and at most {LotLayout.MaxDimension} metres");

    RuleFor(x => x.LengthMeters)
      .GreaterThan(0)
      .LessThanOrEqualTo(LotLayout.MaxDimension)
      .WithMessage($"Length must be greater than 0 and at most {LotLayout.MaxDimension} metres");

    RuleFor(x => x.LevelCount)
      .InclusiveBetween(1, LotLayout.MaxLevelCount)
      .WithMessage($"Level count must be between 1 and {LotLayout.MaxLevelCount}");
  }
}
