using Vogen;

namespace Parkin.Api.Domain.DriverAggregate;

[ValueObject<Guid>]
public readonly partial struct DriverId
{
  private static Validation Validate(Guid value)
      => value != Guid.Empty ? Validation.Ok : Validation.Invalid("DriverId cannot be empty.");
}
