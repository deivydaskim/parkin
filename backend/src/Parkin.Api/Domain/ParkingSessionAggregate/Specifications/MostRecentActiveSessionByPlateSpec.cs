using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Domain.ParkingSessionAggregate.Specifications;

public class MostRecentActiveSessionByPlateSpec : Specification<ParkingSession>
{
  public MostRecentActiveSessionByPlateSpec(ParkingLotId lotId, string normalizedPlate) =>
    Query
        .Where(session =>
          session.LotId == lotId &&
          session.Plate == normalizedPlate &&
          session.Status == SessionStatus.Active)
        .OrderByDescending(session => session.EntryTime);
}
