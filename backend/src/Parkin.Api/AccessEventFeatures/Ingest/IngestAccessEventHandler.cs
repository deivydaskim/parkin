using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.AccessEventAggregate.Specifications;
using Parkin.Api.Domain.AccessGrantAggregate;
using Parkin.Api.Domain.AccessGrantAggregate.Specifications;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;
using Parkin.Api.Domain.Interfaces;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;
using Parkin.Api.Domain.ParkingSessionAggregate;
using Parkin.Api.Domain.ParkingSessionAggregate.Specifications;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Domain.ReservationAggregate.Specifications;
using Parkin.Api.Domain.Services;

namespace Parkin.Api.AccessEventFeatures.Ingest;

public record IngestAccessEventCommand(
  ParkingLotId LotId,
  string RawPlate,
  Direction Direction,
  EventSource Source,
  DateTimeOffset OccurredAt,
  string IdempotencyKey,
  Guid? ActorId) : ICommand<Result<AccessEventDecisionDto>>;

public class IngestAccessEventHandler(
  IRepository<AccessEvent> accessEventRepository,
  IRepository<ParkingSession> sessionRepository,
  IRepository<AuditLogEntry> auditRepository,
  IReadRepository<ParkingLot> lotRepository,
  IReadRepository<Driver> driverRepository,
  IReadRepository<AccessGrant> grantRepository,
  IReadRepository<Reservation> reservationRepository,
  IEntryDecisionService entryDecisionService,
  IOccupancyCalculator occupancyCalculator,
  ILotRowLocker lotRowLocker,
  IUnitOfWork unitOfWork)
  : ICommandHandler<IngestAccessEventCommand, Result<AccessEventDecisionDto>>
{
  public async ValueTask<Result<AccessEventDecisionDto>> Handle(
    IngestAccessEventCommand request, CancellationToken cancellationToken)
  {
    var normalizedPlate = PlateNormalizer.Normalize(request.RawPlate);

    var replay = await FindReplayAsync(request.IdempotencyKey, cancellationToken);
    if (replay is not null) return replay;

    var lot = await lotRepository.FirstOrDefaultAsync(new ParkingLotByIdSpec(request.LotId), cancellationToken);
    if (lot is null)
    {
      return await DenyUnknownLotAsync(request, normalizedPlate, cancellationToken);
    }

    AccessEventDecisionDto? outcome = null;

    try
    {
      await unitOfWork.ExecuteInTransactionAsync(
        async ct => outcome = await IngestAsync(request, lot, normalizedPlate, ct), cancellationToken);
    }
    catch (DbUpdateException)
    {
      // A concurrent request with the same key won the race on ux_access_event_idempotency; return
      // its decision rather than the collision. Any other save failure has no original and rethrows.
      var winner = await FindReplayAsync(request.IdempotencyKey, cancellationToken);
      if (winner is null) throw;
      return winner;
    }

    return outcome!;
  }

  private async Task<AccessEventDecisionDto> IngestAsync(
    IngestAccessEventCommand request, ParkingLot lot, string normalizedPlate, CancellationToken cancellationToken)
  {
    // Without this, two simultaneous entries can both see the same last free space.
    await lotRowLocker.LockAsync(lot.Id, cancellationToken);

    var (matchedPlate, matchedDriver) = await ResolvePlateAsync(normalizedPlate, cancellationToken);

    // A deactivated plate is still recorded as matched, but does not count as known.
    var isPlateKnown = matchedPlate is not null && matchedPlate.Status == PlateStatus.Active;
    var knownDriver = isPlateKnown ? matchedDriver : null;

    if (lot.Status != LotStatus.Active)
    {
      return await RecordDenyAsync(request, lot, normalizedPlate, matchedPlate, matchedDriver,
        DenyReason.LotArchived, cancellationToken);
    }

    return request.Direction == Direction.Exit
      ? await HandleExitAsync(request, lot, normalizedPlate, matchedPlate, matchedDriver, cancellationToken)
      : await HandleEnterAsync(request, lot, normalizedPlate, matchedPlate, matchedDriver, knownDriver,
        isPlateKnown, cancellationToken);
  }

  private async Task<AccessEventDecisionDto> HandleEnterAsync(
    IngestAccessEventCommand request, ParkingLot lot, string normalizedPlate, Plate? matchedPlate,
    Driver? matchedDriver, Driver? knownDriver, bool isPlateKnown, CancellationToken cancellationToken)
  {
    var generalCount = await sessionRepository.CountAsync(
      new ActiveSessionCountByLotPoolSpec(lot.Id, SessionPool.General), cancellationToken);
    var reservedCount = await sessionRepository.CountAsync(
      new ActiveSessionCountByLotPoolSpec(lot.Id, SessionPool.Reserved), cancellationToken);

    var occupancy = occupancyCalculator.Calculate(
      OccupancyContext.Create(lot.Capacity, generalCount, reservedCount));

    var hasActiveGrant = false;
    ParkingSpace? reservedSpace = null;

    if (knownDriver is not null)
    {
      // The spec filters on status only; the validity window is checked against when the event
      // happened, not when we processed it.
      var grants = await grantRepository.ListAsync(
        new ActiveGrantForDriverLotSpec(knownDriver.Id, lot.Id), cancellationToken);
      hasActiveGrant = grants.Any(grant => grant.IsActiveAsOf(request.OccurredAt));

      var reservation = await reservationRepository.FirstOrDefaultAsync(
        new ActiveReservationByDriverLotSpec(knownDriver.Id, lot.Id), cancellationToken);
      if (reservation is not null)
      {
        var space = lot.Spaces.FirstOrDefault(s => s.Id == reservation.SpaceId);
        reservedSpace = space?.Status == SpaceStatus.Active ? space : null;
      }
    }

    var decision = entryDecisionService.Decide(EntryDecisionContext.Create(
      isPlateKnown,
      lot.AccessMode,
      lot.FullBehavior,
      hasActiveGrant,
      reservedSpace is not null,
      occupancy.IsGeneralPoolFull,
      reservedSpace?.Label));

    if (decision.Outcome == EntryDecisionOutcome.Deny)
    {
      return await RecordDenyAsync(request, lot, normalizedPlate, matchedPlate, matchedDriver,
        ToDenyReason(decision.Reason!.Value), cancellationToken);
    }

    var pool = decision.Pool!.Value;
    var accessEvent = AccessEvent.Record(lot.Id, request.RawPlate, normalizedPlate, matchedPlate?.Id,
      matchedDriver?.Id, request.Direction, request.Source, Decision.Allow, denyReason: null,
      request.OccurredAt, request.IdempotencyKey, request.ActorId);

    var session = ParkingSession.OpenForEntry(lot.Id, knownDriver?.Id, normalizedPlate,
      pool == SessionPool.Reserved ? reservedSpace!.Id : null, pool, accessEvent.Id, request.OccurredAt);

    accessEvent.AttachSession(session.Id);

    await sessionRepository.AddAsync(session, cancellationToken);
    await accessEventRepository.AddAsync(accessEvent, cancellationToken);

    return new AccessEventDecisionDto(accessEvent.Id, Decision.Allow, Reason: null, pool,
      decision.ReservedSpaceLabel, session.Id, request.OccurredAt);
  }

  private async Task<AccessEventDecisionDto> HandleExitAsync(
    IngestAccessEventCommand request, ParkingLot lot, string normalizedPlate, Plate? matchedPlate,
    Driver? matchedDriver, CancellationToken cancellationToken)
  {
    var session = await sessionRepository.FirstOrDefaultAsync(
      new MostRecentActiveSessionByPlateSpec(lot.Id, normalizedPlate), cancellationToken);

    if (session is null)
    {
      // No session to close, so occupancy stays put and can never be driven negative.
      return await RecordDenyAsync(request, lot, normalizedPlate, matchedPlate, matchedDriver,
        DenyReason.NoOpenSession, cancellationToken);
    }

    var accessEvent = AccessEvent.Record(lot.Id, request.RawPlate, normalizedPlate, matchedPlate?.Id,
      matchedDriver?.Id, request.Direction, request.Source, Decision.Allow, denyReason: null,
      request.OccurredAt, request.IdempotencyKey, request.ActorId);
    accessEvent.AttachSession(session.Id);

    session.Close(accessEvent.Id, request.OccurredAt);

    await sessionRepository.UpdateAsync(session, cancellationToken);
    await accessEventRepository.AddAsync(accessEvent, cancellationToken);

    return new AccessEventDecisionDto(accessEvent.Id, Decision.Allow, Reason: null, session.Pool,
      ReservedSpaceLabel: null, session.Id, request.OccurredAt);
  }

  private async Task<AccessEventDecisionDto> RecordDenyAsync(
    IngestAccessEventCommand request, ParkingLot lot, string normalizedPlate, Plate? matchedPlate,
    Driver? matchedDriver, DenyReason reason, CancellationToken cancellationToken)
  {
    var accessEvent = AccessEvent.Record(lot.Id, request.RawPlate, normalizedPlate, matchedPlate?.Id,
      matchedDriver?.Id, request.Direction, request.Source, Decision.Deny, reason, request.OccurredAt,
      request.IdempotencyKey, request.ActorId);

    await accessEventRepository.AddAsync(accessEvent, cancellationToken);

    return new AccessEventDecisionDto(accessEvent.Id, Decision.Deny, reason, Pool: null,
      ReservedSpaceLabel: null, SessionId: null, request.OccurredAt);
  }

  // No event row is possible here: access_events.lot_id has no lot to point at. The audit entry
  // hangs off the lot id the gate sent, which is the only useful thing to record.
  private async Task<AccessEventDecisionDto> DenyUnknownLotAsync(
    IngestAccessEventCommand request, string normalizedPlate, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      AuditActorType.Api,
      request.ActorId,
      AuditActions.AccessEventIngested,
      AuditEntityTypes.ParkingLot,
      request.LotId.Value,
      new
      {
        lotId = request.LotId.Value,
        plate = normalizedPlate,
        direction = request.Direction.ToString(),
        source = request.Source.ToString(),
        decision = Decision.Deny.ToString(),
        reason = DenyReason.LotNotFound.ToString()
      });

    await auditRepository.AddAsync(entry, cancellationToken);

    return new AccessEventDecisionDto(EventId: null, Decision.Deny, DenyReason.LotNotFound, Pool: null,
      ReservedSpaceLabel: null, SessionId: null, request.OccurredAt);
  }

  private async Task<(Plate? Plate, Driver? Driver)> ResolvePlateAsync(
    string normalizedPlate, CancellationToken cancellationToken)
  {
    var driver = await driverRepository.FirstOrDefaultAsync(
      new PlateByNormalizedValueSpec(normalizedPlate), cancellationToken);

    var plate = driver?.Plates.FirstOrDefault(p => p.NormalizedPlateNumber == normalizedPlate);
    return (plate, driver);
  }

  // Pool and the reserved label are not columns on the event, so they come back off the session.
  private async Task<AccessEventDecisionDto?> FindReplayAsync(
    string idempotencyKey, CancellationToken cancellationToken)
  {
    var original = await accessEventRepository.FirstOrDefaultAsync(
      new AccessEventByIdempotencyKeySpec(idempotencyKey), cancellationToken);
    if (original is null) return null;

    SessionPool? pool = null;
    string? reservedSpaceLabel = null;

    if (original.SessionId is not null)
    {
      var session = await sessionRepository.FirstOrDefaultAsync(
        new ParkingSessionByIdSpec(original.SessionId.Value), cancellationToken);
      pool = session?.Pool;

      if (session?.SpaceId is not null)
      {
        var lot = await lotRepository.FirstOrDefaultAsync(new ParkingLotByIdSpec(original.LotId), cancellationToken);
        reservedSpaceLabel = lot?.Spaces.FirstOrDefault(s => s.Id == session.SpaceId.Value)?.Label;
      }
    }

    return new AccessEventDecisionDto(original.Id, original.Decision, original.DenyReason, pool,
      reservedSpaceLabel, original.SessionId, original.OccurredAt);
  }

  private static DenyReason ToDenyReason(DecisionReason reason) => reason switch
  {
    DecisionReason.NotAuthorized => DenyReason.NotAuthorized,
    DecisionReason.LotFull => DenyReason.LotFull,
    _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unmapped decision reason.")
  };
}
