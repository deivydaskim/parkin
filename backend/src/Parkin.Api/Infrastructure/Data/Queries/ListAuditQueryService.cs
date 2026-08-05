using Microsoft.EntityFrameworkCore;
using Parkin.Api.AuditFeatures;
using Parkin.Api.AuditFeatures.List;
using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ListAuditQueryService(AppDbContext db) : IListAuditQueryService
{
  private readonly AppDbContext _db = db;

  public async Task<PagedResult<AuditLogEntryDto>> ListAsync(
    int page,
    int perPage,
    DateTimeOffset? from,
    DateTimeOffset? to,
    Guid? actorId,
    AuditActorType? actorType,
    string? entityType)
  {
    var query = _db.AuditLogEntries.AsQueryable();

    if (from.HasValue)
      query = query.Where(e => e.OccurredAt >= from.Value);

    if (to.HasValue)
      query = query.Where(e => e.OccurredAt <= to.Value);

    if (actorId.HasValue)
      query = query.Where(e => e.ActorId == actorId.Value);

    if (actorType.HasValue)
      query = query.Where(e => e.ActorType == actorType.Value);

    if (!string.IsNullOrWhiteSpace(entityType))
      query = query.Where(e => e.EntityType == entityType);

    var items = await query
      .OrderByDescending(e => e.OccurredAt)
      .Skip((page - 1) * perPage)
      .Take(perPage)
      .Select(e => new AuditLogEntryDto(e.Id, e.ActorType, e.ActorId, e.Action, e.EntityType, e.EntityId, e.OccurredAt, e.MetadataJson))
      .AsNoTracking()
      .ToListAsync();

    int totalCount = await query.CountAsync();
    int totalPages = (int)Math.Ceiling(totalCount / (double)perPage);

    return new PagedResult<AuditLogEntryDto>(items, page, perPage, totalCount, totalPages);
  }
}
