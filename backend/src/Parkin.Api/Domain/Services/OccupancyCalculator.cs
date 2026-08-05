namespace Parkin.Api.Domain.Services;

// general free = max(0, capacity - active GENERAL sessions) (architecture doc §5).
// Never mutates a counter - always derived from the two inputs, so it can't drift negative.
public sealed class OccupancyCalculator : IOccupancyCalculator
{
  public OccupancyResult Calculate(OccupancyContext context)
  {
    ArgumentNullException.ThrowIfNull(context);

    var generalUsed = context.ActiveGeneralSessionCount;
    var generalFree = Math.Max(0, context.GeneralCapacity - generalUsed);
    var isFull = generalUsed >= context.GeneralCapacity;
    var isOverCapacity = generalUsed > context.GeneralCapacity;

    return new OccupancyResult
    {
      GeneralCapacity = context.GeneralCapacity,
      GeneralUsed = generalUsed,
      GeneralFree = generalFree,
      IsGeneralPoolFull = isFull,
      IsOverCapacity = isOverCapacity,
      ReservedCount = context.ActiveReservedSessionCount
    };
  }
}
