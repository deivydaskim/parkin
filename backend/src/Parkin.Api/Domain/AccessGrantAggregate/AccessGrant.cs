using Ardalis.GuardClauses;
using Parkin.Api.Domain.AccessGrantAggregate.Events;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Domain.AccessGrantAggregate;

public class AccessGrant : EntityBase<AccessGrant, AccessGrantId>, IAggregateRoot
{
  // Private constructor for EF Core
  private AccessGrant() { }

  private AccessGrant(AccessGrantId id, DriverId driverId, ParkingLotId lotId,
    DateTimeOffset validFrom, DateTimeOffset? validTo, Guid? createdBy)
  {
    if (validTo.HasValue && validTo.Value < validFrom)
    {
      throw new ArgumentException("validTo must not be before validFrom.", nameof(validTo));
    }

    Id = id;
    DriverId = driverId;
    ParkingLotId = lotId;
    ValidFrom = validFrom;
    ValidTo = validTo;
    Status = GrantStatus.Active;
    CreatedBy = createdBy;
    CreatedAt = DateTimeOffset.UtcNow;
  }

  // Factory method for creating new grants (before persistence)
  public static AccessGrant Create(DriverId driverId, ParkingLotId lotId,
    DateTimeOffset? validFrom, DateTimeOffset? validTo, Guid? actorId)
  {
    var grant = new AccessGrant(
      AccessGrantId.From(Guid.NewGuid()), driverId, lotId, validFrom ?? DateTimeOffset.UtcNow, validTo, actorId);
    grant.RegisterDomainEvent(new GrantCreatedEvent(grant.Id, driverId, lotId, actorId));
    return grant;
  }

  public DriverId DriverId { get; private set; }
  public ParkingLotId ParkingLotId { get; private set; }
  public DateTimeOffset ValidFrom { get; private set; }
  public DateTimeOffset? ValidTo { get; private set; }
  public GrantStatus Status { get; private set; }
  public Guid? CreatedBy { get; private set; }
  public DateTimeOffset CreatedAt { get; private set; }

  public bool IsActiveAsOf(DateTimeOffset now)
    => Status == GrantStatus.Active && now >= ValidFrom && (!ValidTo.HasValue || now <= ValidTo.Value);

  public void Revoke(Guid? actorId)
  {
    Status = GrantStatus.Revoked;
    RegisterDomainEvent(new GrantRevokedEvent(Id, actorId));
  }
}
