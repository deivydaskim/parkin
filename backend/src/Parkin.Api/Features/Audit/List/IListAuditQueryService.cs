namespace Parkin.Api.Features.Audit.List;

public interface IListAuditQueryService
{
  Task<PagedResult<AuditLogEntryResponse>> ListAsync(AuditLogFilter filter, CancellationToken cancellationToken);
}
