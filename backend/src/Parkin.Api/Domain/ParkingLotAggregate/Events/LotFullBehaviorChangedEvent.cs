using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class LotFullBehaviorChangedEvent(ParkingLotId lotId, FullBehavior from, FullBehavior to, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.LotFullBehaviorChanged, AuditEntityTypes.ParkingLot, lotId.Value)
{
  public ParkingLotId LotId { get; } = lotId;
  public FullBehavior From { get; } = from;
  public FullBehavior To { get; } = to;

  public override object Metadata => new { from = From.ToString(), to = To.ToString() };
}
