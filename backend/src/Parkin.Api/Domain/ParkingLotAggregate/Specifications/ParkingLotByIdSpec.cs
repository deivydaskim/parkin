namespace Parkin.Api.Domain.ParkingLotAggregate.Specifications;

public class ParkingLotByIdSpec : Specification<ParkingLot>
{
  public ParkingLotByIdSpec(ParkingLotId lotId) =>
    Query
        .Where(lot => lot.Id == lotId);
}
