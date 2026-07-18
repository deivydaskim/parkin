namespace Parkin.Api.Domain.ParkingLotAggregate.Specifications;

public class ParkingLotBySpaceIdSpec : Specification<ParkingLot>
{
  public ParkingLotBySpaceIdSpec(ParkingSpaceId spaceId) =>
    Query
        .Include(lot => lot.Spaces)
        .Where(lot => lot.Spaces.Any(s => s.Id == spaceId));
}
