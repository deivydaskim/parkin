namespace Parkin.Api.AuditFeatures;

public record AuditLogEntryRecord(
  Guid Id,
  string ActorType,
  Guid? ActorId,
  string Action,
  string EntityType,
  Guid EntityId,
  DateTimeOffset OccurredAt,
  string? MetadataJson);
