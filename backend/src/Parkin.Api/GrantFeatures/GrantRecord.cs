using Parkin.Api.Domain.AccessGrantAggregate;

namespace Parkin.Api.GrantFeatures;

public record GrantRecord(
  Guid Id,
  Guid DriverId,
  Guid ParkingLotId,
  DateTimeOffset ValidFrom,
  DateTimeOffset? ValidTo,
  GrantStatus Status);
