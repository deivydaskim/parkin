using Ardalis.GuardClauses;
using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.Services;

namespace Parkin.Api.Domain.ParkingSessionAggregate;

public class ParkingSession : EntityBase<ParkingSession, ParkingSessionId>, IAggregateRoot
{
  // Private constructor for EF Core
  private ParkingSession() { }

  private ParkingSession(ParkingSessionId id, ParkingLotId lotId, DriverId? driverId, string plate,
    ParkingSpaceId? spaceId, SessionPool pool, AccessEventId entryEventId, DateTimeOffset entryTime)
  {
    Guard.Against.NullOrWhiteSpace(plate, nameof(plate));

    Id = id;
    LotId = lotId;
    DriverId = driverId;
    Plate = plate;
    SpaceId = spaceId;
    Pool = pool;
    EntryEventId = entryEventId;
    EntryTime = entryTime;
    Status = SessionStatus.Active;
  }

  public static ParkingSession OpenForEntry(ParkingLotId lotId, DriverId? driverId, string plate,
    ParkingSpaceId? spaceId, SessionPool pool, AccessEventId entryEventId, DateTimeOffset entryTime)
  {
    if (pool == SessionPool.Reserved && spaceId is null)
    {
      throw new ArgumentException("A RESERVED session must occupy a specific space.", nameof(spaceId));
    }

    if (pool == SessionPool.General && spaceId is not null)
    {
      throw new ArgumentException("A GENERAL session occupies the shared pool, not a specific space.", nameof(spaceId));
    }

    return new ParkingSession(ParkingSessionId.From(Guid.NewGuid()), lotId, driverId, plate, spaceId, pool,
      entryEventId, entryTime);
  }

  public ParkingLotId LotId { get; private set; }
  public DriverId? DriverId { get; private set; }
  // Normalized - this is the key an EXIT matches on; the raw string stays on the AccessEvent.
  public string Plate { get; private set; } = string.Empty;
  public ParkingSpaceId? SpaceId { get; private set; }
  public SessionPool Pool { get; private set; }
  public AccessEventId EntryEventId { get; private set; }
  public DateTimeOffset EntryTime { get; private set; }
  public AccessEventId? ExitEventId { get; private set; }
  public DateTimeOffset? ExitTime { get; private set; }
  public SessionStatus Status { get; private set; }

  public void Close(AccessEventId exitEventId, DateTimeOffset exitTime)
  {
    if (Status != SessionStatus.Active)
    {
      throw new InvalidOperationException($"Only an ACTIVE session can be closed; this one is {Status}.");
    }

    ExitEventId = exitEventId;
    ExitTime = exitTime;
    Status = SessionStatus.Closed;
  }
}
