using Vogen;

namespace Parkin.Api.Domain.ParkingLotAggregate;

[ValueObject<Guid>]
public readonly partial struct ParkingLotId
{
  private static Validation Validate(Guid value)
      => value != Guid.Empty ? Validation.Ok : Validation.Invalid("ParkingLotId cannot be empty.");
}
