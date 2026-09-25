namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class LotLayoutAppliedEvent(ParkingLotId lotId, int placedCount, int clearedCount, bool layoutChanged, Guid? actorId)
  : DomainEventBase
{
  public ParkingLotId LotId { get; } = lotId;
  public int PlacedCount { get; } = placedCount;
  public int ClearedCount { get; } = clearedCount;
  public bool LayoutChanged { get; } = layoutChanged;
  public Guid? ActorId { get; } = actorId;
}
