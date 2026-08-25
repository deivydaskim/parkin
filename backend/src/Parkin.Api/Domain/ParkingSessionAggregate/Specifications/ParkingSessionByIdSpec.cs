namespace Parkin.Api.Domain.ParkingSessionAggregate.Specifications;

public class ParkingSessionByIdSpec : Specification<ParkingSession>
{
  public ParkingSessionByIdSpec(ParkingSessionId sessionId) =>
    Query.Where(session => session.Id == sessionId);
}
