using Parkin.Api.Domain.AccessEventAggregate;

namespace Parkin.Api.Features.AccessEvents.Ingest;

public sealed record GateEvent(
  IngestAccessEventCommand Command,
  string NormalizedPlate,
  PlateMatch Plate,
  DateTimeOffset ReceivedAt)
{
  public AccessEvent Allow() => Record(Decision.Allow, denyReason: null);

  public AccessEvent Deny(DenyReason reason) => Record(Decision.Deny, reason);

  private AccessEvent Record(Decision decision, DenyReason? denyReason) => AccessEvent.Record(
    Command.LotId,
    Command.RawPlate,
    NormalizedPlate,
    Plate.PlateId,
    Plate.DriverId,
    Command.Direction,
    Command.Source,
    decision,
    denyReason,
    Command.OccurredAt,
    ReceivedAt,
    Command.IdempotencyKey,
    Command.Actor);
}
