using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.Occupancy;

public record LotOccupancyDto(
  ParkingLotId LotId,
  int GeneralCapacity,
  int GeneralUsed,
  int GeneralFree,
  bool IsGeneralPoolFull,
  bool IsOverCapacity,
  int ReservedSpaceCount,
  int ReservedOccupied,
  DateTimeOffset AsOf,
  string? LotName = null);
