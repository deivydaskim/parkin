using Parkin.Api.Domain.AccessGrantAggregate;

namespace Parkin.Api.Features.Grants;

public record GrantResponse(
  Guid Id,
  Guid DriverId,
  Guid ParkingLotId,
  DateTimeOffset ValidFrom,
  DateTimeOffset? ValidTo,
  GrantStatus Status,
  string? ParkingLotName,
  string? DriverName)
{
  public static GrantResponse From(AccessGrant grant, string? parkingLotName = null, string? driverName = null)
    => new(grant.Id.Value, grant.DriverId.Value, grant.ParkingLotId.Value, grant.ValidFrom, grant.ValidTo,
      grant.Status, parkingLotName, driverName);
}
