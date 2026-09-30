using FluentValidation;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Lots;

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
    RuleFor(x => x).Custom((request, context) => context.AddFailures(request.ToValue()));
  }
}
