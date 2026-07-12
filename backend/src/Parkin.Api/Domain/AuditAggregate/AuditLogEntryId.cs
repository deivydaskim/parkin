using Vogen;

namespace Parkin.Api.Domain.AuditAggregate;

[ValueObject<Guid>]
public readonly partial struct AuditLogEntryId
{
  private static Validation Validate(Guid value)
      => value != Guid.Empty ? Validation.Ok : Validation.Invalid("AuditLogEntryId cannot be empty.");
}
