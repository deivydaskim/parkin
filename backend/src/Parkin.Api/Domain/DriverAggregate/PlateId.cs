using Vogen;

namespace Parkin.Api.Domain.DriverAggregate;

[ValueObject<Guid>]
public readonly partial struct PlateId
{
  private static Validation Validate(Guid value)
      => value != Guid.Empty ? Validation.Ok : Validation.Invalid("PlateId cannot be empty.");
}
