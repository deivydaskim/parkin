using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class LotLayoutAppliedEvent(ParkingLotId lotId, int placedCount, int clearedCount, bool layoutChanged, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.LotLayoutApplied, AuditEntityTypes.ParkingLot, lotId.Value)
{
  public ParkingLotId LotId { get; } = lotId;
  public int PlacedCount { get; } = placedCount;
  public int ClearedCount { get; } = clearedCount;
  public bool LayoutChanged { get; } = layoutChanged;

  public override object Metadata => new { placed = PlacedCount, cleared = ClearedCount, layoutChanged = LayoutChanged };
}
