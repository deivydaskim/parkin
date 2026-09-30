namespace Parkin.Api.Domain.Services;

public sealed record OccupancyResult
{
  public required int GeneralCapacity { get; init; }
  public required int GeneralUsed { get; init; }
  public required int GeneralFree { get; init; }
  public required bool IsGeneralPoolFull { get; init; }
  public required bool IsOverCapacity { get; init; }
  public required int ReservedCount { get; init; }

  public static OccupancyResult Calculate(OccupancyContext context)
  {
    ArgumentNullException.ThrowIfNull(context);

    var generalUsed = context.ActiveGeneralSessionCount;

    return new OccupancyResult
    {
      GeneralCapacity = context.GeneralCapacity,
      GeneralUsed = generalUsed,
      GeneralFree = Math.Max(0, context.GeneralCapacity - generalUsed),
      IsGeneralPoolFull = generalUsed >= context.GeneralCapacity,
      IsOverCapacity = generalUsed > context.GeneralCapacity,
      ReservedCount = context.ActiveReservedSessionCount
    };
  }
}
