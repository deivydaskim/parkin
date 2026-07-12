namespace Parkin.Api.Domain.ParkingLotAggregate.Specifications;

public class ParkingLotByNameSpec : Specification<ParkingLot>
{
  public ParkingLotByNameSpec(string name) =>
    Query
        .Where(lot => lot.Name == name);
}
