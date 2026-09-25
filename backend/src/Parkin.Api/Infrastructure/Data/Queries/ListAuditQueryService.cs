using Microsoft.EntityFrameworkCore;
using Parkin.Api.AuditFeatures;
using Parkin.Api.AuditFeatures.List;
using Parkin.Api.Domain.ApiKeyAggregate;
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

    var actorNames = await ResolveActorNamesAsync(items);
    var named = items
      .Select(e => e.ActorId is { } id && actorNames.TryGetValue(id, out var name) ? e with { ActorName = name } : e)
      .ToList();

    int totalCount = await query.CountAsync();
    int totalPages = (int)Math.Ceiling(totalCount / (double)perPage);

    return new PagedResult<AuditLogEntryDto>(named, page, perPage, totalCount, totalPages);
  }

  private async Task<Dictionary<Guid, string>> ResolveActorNamesAsync(IReadOnlyCollection<AuditLogEntryDto> entries)
  {
    var staffIds = entries
      .Where(e => e.ActorType == AuditActorType.Staff && e.ActorId.HasValue)
      .Select(e => e.ActorId!.Value)
      .Distinct()
      .ToList();
    var apiKeyIds = entries
      .Where(e => e.ActorType == AuditActorType.Api && e.ActorId.HasValue)
      .Select(e => ApiKeyId.From(e.ActorId!.Value))
      .Distinct()
      .ToList();

    var names = new Dictionary<Guid, string>();

    if (staffIds.Count > 0)
    {
      var staff = await _db.Users
        .Where(u => staffIds.Contains(u.Id))
        .Select(u => new { u.Id, u.DisplayName })
        .AsNoTracking()
        .ToListAsync();
      foreach (var user in staff) names[user.Id] = user.DisplayName;
    }

    if (apiKeyIds.Count > 0)
    {
      var keys = await _db.ApiKeys
        .Where(k => apiKeyIds.Contains(k.Id))
        .Select(k => new { k.Id, k.Name })
        .AsNoTracking()
        .ToListAsync();
      foreach (var key in keys) names[key.Id.Value] = key.Name;
    }

    return names;
  }
}
