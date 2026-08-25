namespace Parkin.Api.OccupancyFeatures;

// ReservedSpaceCount (active RESERVED spaces) and ReservedOccupied (active RESERVED sessions) are
// different numbers; the panel needs both.
public record LotOccupancyRecord(
  Guid LotId,
  int GeneralCapacity,
  int GeneralUsed,
  int GeneralFree,
  bool IsGeneralPoolFull,
  bool IsOverCapacity,
  int ReservedSpaceCount,
  int ReservedOccupied,
  DateTimeOffset AsOf);
