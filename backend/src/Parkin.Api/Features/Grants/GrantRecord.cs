using Parkin.Api.Domain.AccessGrantAggregate;

namespace Parkin.Api.Features.Grants;

public record GrantRecord(
  Guid Id,
  Guid DriverId,
  Guid ParkingLotId,
  DateTimeOffset ValidFrom,
  DateTimeOffset? ValidTo,
  GrantStatus Status,
  string? ParkingLotName,
  string? DriverName);
