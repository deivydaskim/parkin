using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.AuditFeatures;

public record AuditLogEntryDto(
  AuditLogEntryId Id,
  AuditActorType ActorType,
  Guid? ActorId,
  string Action,
  string EntityType,
  Guid EntityId,
  DateTimeOffset OccurredAt,
  string? MetadataJson);
