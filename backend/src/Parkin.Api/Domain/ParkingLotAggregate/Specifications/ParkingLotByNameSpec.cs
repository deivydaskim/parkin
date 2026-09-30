namespace Parkin.Api.Domain.ParkingLotAggregate.Specifications;

public class ParkingLotByNameSpec : Specification<ParkingLot>
{
  public ParkingLotByNameSpec(string name, ParkingLotId? excludingLotId = null)
  {
    Query.Where(lot => lot.Name == name);

    if (excludingLotId is { } excluded)
    {
      Query.Where(lot => lot.Id != excluded);
    }
  }
}
