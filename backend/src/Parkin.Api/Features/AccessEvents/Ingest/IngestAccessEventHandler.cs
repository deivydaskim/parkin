using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.Interfaces;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingSessionAggregate;
using Parkin.Api.Domain.ParkingSessionAggregate.Specifications;
using Parkin.Api.Domain.Services;

namespace Parkin.Api.Features.AccessEvents.Ingest;

public record IngestAccessEventCommand(
  ParkingLotId LotId,
  string RawPlate,
  Direction Direction,
  EventSource Source,
  DateTimeOffset OccurredAt,
  string IdempotencyKey,
  AccessEventActor Actor) : ICommand<Result<AccessEventDecisionResponse>>;

public class IngestAccessEventHandler(
  AccessEventIdempotency idempotency,
  IUnitOfWork unitOfWork,
  IGateLotReader gateLotReader,
  EntryContextBuilder entryContextBuilder,
  IRepository<AccessEvent> accessEventRepository,
  IRepository<ParkingSession> sessionRepository,
  IRepository<AuditLogEntry> auditRepository,
  TimeProvider timeProvider)
  : ICommandHandler<IngestAccessEventCommand, Result<AccessEventDecisionResponse>>
{
  public async ValueTask<Result<AccessEventDecisionResponse>> Handle(
    IngestAccessEventCommand command, CancellationToken cancellationToken)
    => await idempotency.RunOnceAsync(command, ct => IngestInTransactionAsync(command, ct), cancellationToken);

  private async Task<Result<AccessEventDecisionResponse>> IngestInTransactionAsync(
    IngestAccessEventCommand command, CancellationToken cancellationToken)
  {
    Result<AccessEventDecisionResponse>? result = null;
    await unitOfWork.ExecuteInTransactionAsync(
      async ct => result = await IngestAsync(command, ct), cancellationToken);
    return result!;
  }

  private async Task<Result<AccessEventDecisionResponse>> IngestAsync(
    IngestAccessEventCommand command, CancellationToken cancellationToken)
  {
    var lot = await gateLotReader.LockAndLoadAsync(command.LotId, cancellationToken);
    if (lot is null)
    {
      if (command.Actor.IsStaff) return Result.NotFound();
      return await DenyUnknownLotAsync(command, cancellationToken);
    }

    var normalizedPlate = PlateNormalizer.Normalize(command.RawPlate);
    var plate = await entryContextBuilder.MatchPlateAsync(normalizedPlate, cancellationToken);
    var gateEvent = new GateEvent(command, normalizedPlate, plate, timeProvider.GetUtcNow());

    if (lot.Status != LotStatus.Active)
    {
      return await DenyAsync(gateEvent, DenyReason.LotArchived, cancellationToken);
    }

    return command.Direction == Direction.Exit
      ? await ExitAsync(gateEvent, cancellationToken)
      : await EnterAsync(gateEvent, lot, cancellationToken);
  }

  private async Task<AccessEventDecisionResponse> EnterAsync(GateEvent gateEvent, GateLot lot,
    CancellationToken cancellationToken)
  {
    var entry = await entryContextBuilder.BuildAsync(lot, gateEvent.Plate, gateEvent.Command.OccurredAt,
      cancellationToken);
    var decision = EntryDecision.Decide(entry.DecisionContext);

    if (decision.Outcome == EntryDecisionOutcome.Deny)
    {
      return await DenyAsync(gateEvent, ToDenyReason(decision.Reason!.Value), cancellationToken);
    }

    var pool = decision.Pool!.Value;
    var accessEvent = gateEvent.Allow();
    var session = ParkingSession.OpenForEntry(lot.Id, gateEvent.Plate.KnownDriverId, gateEvent.NormalizedPlate,
      pool == SessionPool.Reserved ? entry.ReservedSpaceId : null, pool, accessEvent.Id,
      gateEvent.Command.OccurredAt);
    accessEvent.AttachSession(session.Id);

    await sessionRepository.AddAsync(session, cancellationToken);
    await accessEventRepository.AddAsync(accessEvent, cancellationToken);

    return AccessEventDecisionResponse.From(accessEvent, pool, decision.ReservedSpaceLabel);
  }

  private async Task<AccessEventDecisionResponse> ExitAsync(GateEvent gateEvent, CancellationToken cancellationToken)
  {
    var session = await sessionRepository.FirstOrDefaultAsync(
      new MostRecentActiveSessionByPlateSpec(gateEvent.Command.LotId, gateEvent.NormalizedPlate), cancellationToken);
    if (session is null)
    {
      return await DenyAsync(gateEvent, DenyReason.NoOpenSession, cancellationToken);
    }

    var accessEvent = gateEvent.Allow();
    accessEvent.AttachSession(session.Id);
    session.Close(accessEvent.Id, gateEvent.Command.OccurredAt);

    await sessionRepository.UpdateAsync(session, cancellationToken);
    await accessEventRepository.AddAsync(accessEvent, cancellationToken);

    return AccessEventDecisionResponse.From(accessEvent, session.Pool);
  }

  private async Task<AccessEventDecisionResponse> DenyAsync(GateEvent gateEvent, DenyReason reason,
    CancellationToken cancellationToken)
  {
    var accessEvent = gateEvent.Deny(reason);
    await accessEventRepository.AddAsync(accessEvent, cancellationToken);
    return AccessEventDecisionResponse.From(accessEvent);
  }

  private async Task<AccessEventDecisionResponse> DenyUnknownLotAsync(IngestAccessEventCommand command,
    CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      AuditActorType.Api,
      command.Actor.Id,
      AuditActions.AccessEventIngested,
      AuditEntityTypes.ParkingLot,
      command.LotId.Value,
      timeProvider.GetUtcNow(),
      new
      {
        lotId = command.LotId.Value,
        plate = PlateNormalizer.Normalize(command.RawPlate),
        direction = command.Direction.ToString(),
        source = command.Source.ToString(),
        decision = Decision.Deny.ToString(),
        reason = DenyReason.LotNotFound.ToString()
      });

    await auditRepository.AddAsync(entry, cancellationToken);

    return AccessEventDecisionResponse.LotNotFound(command.OccurredAt);
  }

  private static DenyReason ToDenyReason(DecisionReason reason) => reason switch
  {
    DecisionReason.NotAuthorized => DenyReason.NotAuthorized,
    DecisionReason.LotFull => DenyReason.LotFull,
    _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unmapped decision reason.")
  };
}
