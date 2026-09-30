using FluentValidation;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Spaces;

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
    RuleFor(x => x).Custom((request, context) => context.AddFailures(request.ToValue()));
  }
}
