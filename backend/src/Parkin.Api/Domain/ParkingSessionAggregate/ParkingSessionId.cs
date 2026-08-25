using Vogen;

namespace Parkin.Api.Domain.ParkingSessionAggregate;

[ValueObject<Guid>]
public readonly partial struct ParkingSessionId
{
  private static Validation Validate(Guid value)
      => value != Guid.Empty ? Validation.Ok : Validation.Invalid("ParkingSessionId cannot be empty.");
}
