using Vogen;

namespace Parkin.Api.Domain.ReservationAggregate;

[ValueObject<Guid>]
public readonly partial struct ReservationId
{
  private static Validation Validate(Guid value)
      => value != Guid.Empty ? Validation.Ok : Validation.Invalid("ReservationId cannot be empty.");
}
