using Vogen;

namespace Parkin.Api.Domain.ParkingLotAggregate;

[ValueObject<Guid>]
public readonly partial struct ParkingSpaceId
{
  private static Validation Validate(Guid value)
      => value != Guid.Empty ? Validation.Ok : Validation.Invalid("ParkingSpaceId cannot be empty.");
}
