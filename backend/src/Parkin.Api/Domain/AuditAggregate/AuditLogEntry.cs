using System.Text.Json;
using Ardalis.GuardClauses;

namespace Parkin.Api.Domain.AuditAggregate;

public class AuditLogEntry : EntityBase<AuditLogEntry, AuditLogEntryId>, IAggregateRoot
{
  private AuditLogEntry() { }

  private AuditLogEntry(AuditLogEntryId id, AuditActorType actorType, Guid? actorId, string action,
    string entityType, Guid entityId, DateTimeOffset occurredAt, string? metadataJson)
  {
    Guard.Against.NullOrWhiteSpace(action, nameof(action));
    Guard.Against.NullOrWhiteSpace(entityType, nameof(entityType));

    Id = id;
    ActorType = actorType;
    ActorId = actorId;
    Action = action;
    EntityType = entityType;
    EntityId = entityId;
    OccurredAt = occurredAt;
    MetadataJson = metadataJson;
  }

  public static AuditLogEntry FromEvent(AuditableDomainEvent domainEvent, DateTimeOffset occurredAt)
    => Create(domainEvent.ActorType, domainEvent.ActorId, domainEvent.Action, domainEvent.EntityType,
      domainEvent.EntityId, occurredAt, domainEvent.Metadata);

  public static AuditLogEntry Create(AuditActorType actorType, Guid? actorId, string action,
    string entityType, Guid entityId, DateTimeOffset occurredAt, object? metadata = null)
    => new(AuditLogEntryId.From(Guid.CreateVersion7()), actorType, actorId, action, entityType, entityId,
      occurredAt, metadata is null ? null : JsonSerializer.Serialize(metadata));

  public AuditActorType ActorType { get; private set; }
  public Guid? ActorId { get; private set; }
  public string Action { get; private set; } = string.Empty;
  public string EntityType { get; private set; } = string.Empty;
  public Guid EntityId { get; private set; }
  public DateTimeOffset OccurredAt { get; private set; }
  public string? MetadataJson { get; private set; }
}
