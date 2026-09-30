using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.AccessEvents.Ingest;

public interface IGateLotReader
{
  Task<GateLot?> LockAndLoadAsync(ParkingLotId lotId, CancellationToken cancellationToken);

  Task<string?> FindActiveSpaceLabelAsync(ParkingLotId lotId, ParkingSpaceId spaceId,
    CancellationToken cancellationToken);
}

public sealed record GateLot(
  ParkingLotId Id,
  LotStatus Status,
  AccessMode AccessMode,
  FullBehavior FullBehavior,
  int GeneralCapacity,
  int ActiveGeneralSessions,
  int ActiveReservedSessions);
