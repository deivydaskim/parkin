using Vogen;

namespace Parkin.Api.Domain.AccessGrantAggregate;

[ValueObject<Guid>]
public readonly partial struct AccessGrantId
{
  private static Validation Validate(Guid value)
      => value != Guid.Empty ? Validation.Ok : Validation.Invalid("AccessGrantId cannot be empty.");
}
