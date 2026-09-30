using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.Exceptions;

namespace Parkin.Api.Features.AccessEvents.Ingest;

public class AccessEventIdempotency(IAccessEventReplayQueryService replayQuery)
{
  public const string KeyReusedMessage =
    "This Idempotency-Key was already used for a different access event (lot, plate or direction differ).";

  public async Task<Result<AccessEventDecisionResponse>> RunOnceAsync(IngestAccessEventCommand command,
    Func<CancellationToken, Task<Result<AccessEventDecisionResponse>>> ingest, CancellationToken cancellationToken)
  {
    var replay = await ReplayAsync(command, cancellationToken);
    if (replay is not null) return replay;

    try
    {
      return await ingest(cancellationToken);
    }
    catch (UniqueConstraintViolationException ex) when (ex.ConstraintName == AccessEvent.IdempotencyIndexName)
    {
      var winner = await ReplayAsync(command, cancellationToken);
      if (winner is null) throw;
      return winner;
    }
  }

  private async Task<Result<AccessEventDecisionResponse>?> ReplayAsync(IngestAccessEventCommand command,
    CancellationToken cancellationToken)
  {
    var recorded = await replayQuery.FindAsync(command.Actor.Id, command.IdempotencyKey, cancellationToken);
    if (recorded is null) return null;

    var normalizedPlate = PlateNormalizer.Normalize(command.RawPlate);
    return recorded.IsSameEventAs(command.LotId, normalizedPlate, command.Direction)
      ? Result.Success(recorded.Decision)
      : Result<AccessEventDecisionResponse>.Conflict(KeyReusedMessage);
  }
}
