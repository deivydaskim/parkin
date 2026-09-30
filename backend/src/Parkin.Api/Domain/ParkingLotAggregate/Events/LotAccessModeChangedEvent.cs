using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class LotAccessModeChangedEvent(ParkingLotId lotId, AccessMode from, AccessMode to, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.LotAccessModeChanged, AuditEntityTypes.ParkingLot, lotId.Value)
{
  public ParkingLotId LotId { get; } = lotId;
  public AccessMode From { get; } = from;
  public AccessMode To { get; } = to;

  public override object Metadata => new { from = From.ToString(), to = To.ToString() };
}
