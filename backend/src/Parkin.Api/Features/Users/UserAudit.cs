using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Features.Users;

internal static class UserAudit
{
  public static AuditLogEntry Entry(string action, Guid? actorId, Guid userId, TimeProvider timeProvider,
    object? metadata = null)
    => AuditLogEntry.Create(AuditActorType.Staff, actorId, action, AuditEntityTypes.User, userId,
      timeProvider.GetUtcNow(), metadata);
}
