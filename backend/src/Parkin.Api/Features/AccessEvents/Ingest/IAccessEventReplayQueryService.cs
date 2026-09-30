using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.AccessEvents.Ingest;

public interface IAccessEventReplayQueryService
{
  Task<RecordedAccessEvent?> FindAsync(Guid actorId, string idempotencyKey, CancellationToken cancellationToken);
}

public sealed record RecordedAccessEvent(
  ParkingLotId LotId,
  string NormalizedPlate,
  Direction Direction,
  AccessEventDecisionResponse Decision)
{
  public bool IsSameEventAs(ParkingLotId lotId, string normalizedPlate, Direction direction)
    => LotId == lotId && NormalizedPlate == normalizedPlate && Direction == direction;
}
