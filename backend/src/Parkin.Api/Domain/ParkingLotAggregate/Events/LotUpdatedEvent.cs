using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class LotUpdatedEvent(ParkingLotId lotId, IReadOnlyDictionary<string, LotFieldChange> changes, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.LotUpdated, AuditEntityTypes.ParkingLot, lotId.Value)
{
  public ParkingLotId LotId { get; } = lotId;
  public IReadOnlyDictionary<string, LotFieldChange> Changes { get; } = changes;

  public override object Metadata => new
  {
    changes = Changes.ToDictionary(change => change.Key, change => new { from = change.Value.From, to = change.Value.To })
  };
}
