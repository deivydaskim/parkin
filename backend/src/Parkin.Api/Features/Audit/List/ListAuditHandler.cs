using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Features.Audit.List;

public record AuditLogFilter(
  int Page,
  int PerPage,
  DateTimeOffset? From,
  DateTimeOffset? To,
  Guid? ActorId,
  AuditActorType? ActorType,
  string? EntityType);

public record ListAuditQuery(AuditLogFilter Filter) : IQuery<Result<PagedResult<AuditLogEntryResponse>>>;

public class ListAuditHandler(IListAuditQueryService query)
  : IQueryHandler<ListAuditQuery, Result<PagedResult<AuditLogEntryResponse>>>
{
  public async ValueTask<Result<PagedResult<AuditLogEntryResponse>>> Handle(ListAuditQuery request,
    CancellationToken cancellationToken)
    => await query.ListAsync(request.Filter, cancellationToken);
}
