namespace Parkin.Api.AuditFeatures;

internal static class AuditMapping
{
  public static AuditLogEntryRecord ToRecord(AuditLogEntryDto dto) => new(
    dto.Id.Value,
    dto.ActorType.ToString(),
    dto.ActorId,
    dto.Action,
    dto.EntityType,
    dto.EntityId,
    dto.OccurredAt,
    dto.MetadataJson,
    dto.ActorName);
}
