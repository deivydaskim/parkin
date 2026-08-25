using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.Services;

namespace Parkin.Api.Domain.ParkingSessionAggregate.Specifications;

public class ActiveSessionCountByLotPoolSpec : Specification<ParkingSession>
{
  public ActiveSessionCountByLotPoolSpec(ParkingLotId lotId, SessionPool pool)
  {
    LotId = lotId;
    Pool = pool;

    Query
        .Where(session =>
          session.LotId == lotId &&
          session.Pool == pool &&
          session.Status == SessionStatus.Active);
  }

  public ParkingLotId LotId { get; }
  public SessionPool Pool { get; }
}
