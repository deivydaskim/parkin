using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Domain.AccessGrantAggregate.Specifications;

public class ActiveGrantForDriverLotSpec : Specification<AccessGrant>
{
  public ActiveGrantForDriverLotSpec(DriverId driverId, ParkingLotId lotId) =>
    Query
        .Where(grant =>
          grant.DriverId == driverId &&
          grant.ParkingLotId == lotId &&
          grant.Status == GrantStatus.Active);
}
