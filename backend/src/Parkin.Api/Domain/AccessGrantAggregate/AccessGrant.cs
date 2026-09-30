using Parkin.Api.Domain.AccessGrantAggregate.Events;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Domain.AccessGrantAggregate;

public class AccessGrant : EntityBase<AccessGrant, AccessGrantId>, IAggregateRoot
{
  private AccessGrant() { }

  private AccessGrant(AccessGrantId id, DriverId driverId, ParkingLotId lotId,
    DateTimeOffset validFrom, DateTimeOffset? validTo, Guid? createdBy, DateTimeOffset createdAt)
  {
    Id = id;
    DriverId = driverId;
    ParkingLotId = lotId;
    ValidFrom = validFrom;
    ValidTo = validTo;
    Status = GrantStatus.Active;
    CreatedBy = createdBy;
    CreatedAt = createdAt;
  }

  public static Result<AccessGrant> Create(DriverId driverId, ParkingLotId lotId,
    DateTimeOffset? validFrom, DateTimeOffset? validTo, DateTimeOffset now, Guid? actorId)
  {
    var effectiveValidFrom = validFrom ?? now;
    if (validTo < effectiveValidFrom)
    {
      return Result.Invalid(new ValidationError("ValidTo", "Valid-to must not be before valid-from"));
    }

    var grant = new AccessGrant(
      AccessGrantId.From(Guid.CreateVersion7()), driverId, lotId, effectiveValidFrom, validTo, actorId, now);
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

  public Result Revoke(Guid? actorId)
  {
    if (Status == GrantStatus.Revoked)
    {
      return Result.Invalid(new ValidationError("GrantId", "Grant is already revoked"));
    }

    Status = GrantStatus.Revoked;
    RegisterDomainEvent(new GrantRevokedEvent(Id, actorId));
    return Result.Success();
  }
}
