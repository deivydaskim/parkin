using System.Text.Json;
using Ardalis.GuardClauses;

namespace Parkin.Api.Domain.AuditAggregate;

public class AuditLogEntry : EntityBase<AuditLogEntry, AuditLogEntryId>, IAggregateRoot
{
  // Private constructor for EF Core
  private AuditLogEntry() { }

  private AuditLogEntry(AuditLogEntryId id, AuditActorType actorType, Guid? actorId, string action,
    string entityType, Guid entityId, string? metadataJson)
  {
    Guard.Against.NullOrWhiteSpace(action, nameof(action));
    Guard.Against.NullOrWhiteSpace(entityType, nameof(entityType));

    Id = id;
    ActorType = actorType;
    ActorId = actorId;
    Action = action;
    EntityType = entityType;
    EntityId = entityId;
    OccurredAt = DateTimeOffset.UtcNow;
    MetadataJson = metadataJson;
  }

  public static AuditLogEntry Create(AuditActorType actorType, Guid? actorId, string action,
    string entityType, Guid entityId, object? metadata = null)
    => new(AuditLogEntryId.From(Guid.NewGuid()), actorType, actorId, action, entityType, entityId,
      metadata is null ? null : JsonSerializer.Serialize(metadata));

  public AuditActorType ActorType { get; private set; }
  public Guid? ActorId { get; private set; }
  public string Action { get; private set; } = string.Empty;
  public string EntityType { get; private set; } = string.Empty;
  public Guid EntityId { get; private set; }
  public DateTimeOffset OccurredAt { get; private set; }
  public string? MetadataJson { get; private set; }
}
