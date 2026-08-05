using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.AuditFeatures.List;

public interface IListAuditQueryService
{
  Task<PagedResult<AuditLogEntryDto>> ListAsync(
    int page,
    int perPage,
    DateTimeOffset? from,
    DateTimeOffset? to,
    Guid? actorId,
    AuditActorType? actorType,
    string? entityType);
}
