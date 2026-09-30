using Parkin.Api.Domain.Services;

namespace Parkin.Api.Features.Occupancy;

public record LotOccupancyResponse(
  Guid LotId,
  int GeneralCapacity,
  int GeneralUsed,
  int GeneralFree,
  bool IsGeneralPoolFull,
  bool IsOverCapacity,
  int ReservedSpaceCount,
  int ReservedOccupied,
  DateTimeOffset AsOf,
  string? LotName)
{
  public static LotOccupancyResponse From(LotOccupancyInputs lot, DateTimeOffset asOf)
  {
    var occupancy = OccupancyResult.Calculate(
      OccupancyContext.Create(lot.GeneralCapacity, lot.ActiveGeneralSessions, lot.ActiveReservedSessions));

    return new LotOccupancyResponse(
      lot.LotId.Value,
      occupancy.GeneralCapacity,
      occupancy.GeneralUsed,
      occupancy.GeneralFree,
      occupancy.IsGeneralPoolFull,
      occupancy.IsOverCapacity,
      lot.ReservedSpaceCount,
      occupancy.ReservedCount,
      asOf,
      lot.LotName);
  }
}
