using Vogen;

namespace Parkin.Api.Domain.AccessEventAggregate;

[ValueObject<Guid>]
public readonly partial struct AccessEventId
{
  private static Validation Validate(Guid value)
      => value != Guid.Empty ? Validation.Ok : Validation.Invalid("AccessEventId cannot be empty.");
}
