using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.ApiKeyAggregate;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Features.Audit;
using Parkin.Api.Features.Audit.List;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ListAuditQueryService(AppDbContext db) : IListAuditQueryService
{
  public async Task<PagedResult<AuditLogEntryResponse>> ListAsync(AuditLogFilter filter,
    CancellationToken cancellationToken)
  {
    var query = Filter(db.AuditLogEntries.AsNoTracking(), filter);

    var entries = await query
      .OrderByDescending(e => e.OccurredAt)
      .Skip((filter.Page - 1) * filter.PerPage)
      .Take(filter.PerPage)
      .ToListAsync(cancellationToken);

    var actorNames = await ResolveActorNamesAsync(entries, cancellationToken);
    var items = entries
      .Select(e => new AuditLogEntryResponse(e.Id.Value, e.ActorType.ToString(), e.ActorId, e.Action, e.EntityType, e.EntityId,
        e.OccurredAt, e.MetadataJson,
        e.ActorId is { } actorId && actorNames.TryGetValue(actorId, out var name) ? name : null))
      .ToList();

    var totalCount = await query.CountAsync(cancellationToken);
    var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PerPage);

    return new PagedResult<AuditLogEntryResponse>(items, filter.Page, filter.PerPage, totalCount, totalPages);
  }

  private static IQueryable<AuditLogEntry> Filter(IQueryable<AuditLogEntry> query, AuditLogFilter filter)
  {
    if (filter.From.HasValue)
      query = query.Where(e => e.OccurredAt >= filter.From.Value);

    if (filter.To.HasValue)
      query = query.Where(e => e.OccurredAt <= filter.To.Value);

    if (filter.ActorId.HasValue)
      query = query.Where(e => e.ActorId == filter.ActorId.Value);

    if (filter.ActorType.HasValue)
      query = query.Where(e => e.ActorType == filter.ActorType.Value);

    if (!string.IsNullOrWhiteSpace(filter.EntityType))
      query = query.Where(e => e.EntityType == filter.EntityType);

    return query;
  }

  private async Task<Dictionary<Guid, string>> ResolveActorNamesAsync(IReadOnlyCollection<AuditLogEntry> entries,
    CancellationToken cancellationToken)
  {
    var staffIds = ActorIds(entries, AuditActorType.Staff);
    var apiKeyIds = ActorIds(entries, AuditActorType.Api).Select(ApiKeyId.From).ToList();

    var names = new Dictionary<Guid, string>();

    if (staffIds.Count > 0)
    {
      var staff = await db.Users
        .Where(u => staffIds.Contains(u.Id))
        .Select(u => new { u.Id, u.DisplayName })
        .AsNoTracking()
        .ToListAsync(cancellationToken);
      foreach (var user in staff) names[user.Id] = user.DisplayName;
    }

    if (apiKeyIds.Count > 0)
    {
      var keys = await db.ApiKeys
        .Where(k => apiKeyIds.Contains(k.Id))
        .Select(k => new { k.Id, k.Name })
        .AsNoTracking()
        .ToListAsync(cancellationToken);
      foreach (var key in keys) names[key.Id.Value] = key.Name;
    }

    return names;
  }

  private static List<Guid> ActorIds(IEnumerable<AuditLogEntry> entries, AuditActorType actorType)
    => entries
      .Where(e => e.ActorType == actorType && e.ActorId.HasValue)
      .Select(e => e.ActorId!.Value)
      .Distinct()
      .ToList();
}
