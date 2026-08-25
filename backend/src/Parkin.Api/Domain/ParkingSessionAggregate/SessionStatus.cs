namespace Parkin.Api.Domain.ParkingSessionAggregate;

// Expired is reserved for the P1 stale-session auto-expiry job (E8); nothing sets it in V1.
public enum SessionStatus
{
  Active,
  Closed,
  Expired
}
