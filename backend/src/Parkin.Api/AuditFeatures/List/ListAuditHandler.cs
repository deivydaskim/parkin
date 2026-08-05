using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.AuditFeatures.List;

public record ListAuditQuery(
  int? Page,
  int? PerPage,
  DateTimeOffset? From,
  DateTimeOffset? To,
  Guid? Actor,
  AuditActorType? ActorType,
  string? Entity)
  : IQuery<Result<PagedResult<AuditLogEntryDto>>>;

public class ListAuditHandler(IListAuditQueryService query) : IQueryHandler<ListAuditQuery, Result<PagedResult<AuditLogEntryDto>>>
{
  private readonly IListAuditQueryService _query = query;

  public async ValueTask<Result<PagedResult<AuditLogEntryDto>>> Handle(ListAuditQuery request,
                                                                        CancellationToken cancellationToken)
  {
    var result = await _query.ListAsync(
      request.Page ?? 1,
      request.PerPage ?? Constants.DEFAULT_PAGE_SIZE,
      request.From,
      request.To,
      request.Actor,
      request.ActorType,
      request.Entity);

    return Result.Success(result);
  }
}
