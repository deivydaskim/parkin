using Parkin.Api.Domain.AccessGrantAggregate;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.Grants;

public record GrantDto(
  AccessGrantId Id,
  DriverId DriverId,
  ParkingLotId ParkingLotId,
  DateTimeOffset ValidFrom,
  DateTimeOffset? ValidTo,
  GrantStatus Status,
  string? ParkingLotName = null,
  string? DriverName = null);
