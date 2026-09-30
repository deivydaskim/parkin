using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.Occupancy;

public record LotOccupancyInputs(
  ParkingLotId LotId,
  string LotName,
  int GeneralCapacity,
  int ReservedSpaceCount,
  int ActiveGeneralSessions,
  int ActiveReservedSessions);
