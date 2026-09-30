namespace Parkin.Api.Domain.Services;

public sealed record OccupancyContext
{
  public int GeneralCapacity { get; private init; }
  public int ActiveGeneralSessionCount { get; private init; }
  public int ActiveReservedSessionCount { get; private init; }

  public static OccupancyContext Create(int generalCapacity, int activeGeneralSessionCount, int activeReservedSessionCount = 0)
  {
    if (generalCapacity < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(generalCapacity), generalCapacity, "General capacity cannot be negative.");
    }

    if (activeGeneralSessionCount < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(activeGeneralSessionCount), activeGeneralSessionCount, "Active GENERAL session count cannot be negative.");
    }

    if (activeReservedSessionCount < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(activeReservedSessionCount), activeReservedSessionCount, "Active RESERVED session count cannot be negative.");
    }

    return new OccupancyContext
    {
      GeneralCapacity = generalCapacity,
      ActiveGeneralSessionCount = activeGeneralSessionCount,
      ActiveReservedSessionCount = activeReservedSessionCount
    };
  }
}
