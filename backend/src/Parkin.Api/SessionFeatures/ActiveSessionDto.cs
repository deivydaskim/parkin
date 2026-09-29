using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingSessionAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.Services;

namespace Parkin.Api.SessionFeatures;

public record ActiveSessionDto(
  ParkingSessionId Id,
  string Plate,
  DriverId? DriverId,
  string? DriverName,
  SessionPool Pool,
  ParkingSpaceId? SpaceId,
  string? SpaceLabel,
  DateTimeOffset EntryTime);
