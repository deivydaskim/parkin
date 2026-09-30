using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Domain.ApiKeyAggregate.Events;

public class ApiKeyCreatedEvent(ApiKeyId apiKeyId, ParkingLotId lotId, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.ApiKeyCreate, AuditEntityTypes.ApiKey, apiKeyId.Value)
{
  public ApiKeyId ApiKeyId { get; } = apiKeyId;
  public ParkingLotId LotId { get; } = lotId;

  public override object Metadata => new { lotId = LotId.Value };
}
