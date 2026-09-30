using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.Services;

namespace Parkin.Api.Features.Sessions.ListActiveByLot;

public interface IListActiveSessionsByLotQueryService
{
  Task<PagedResult<ActiveSessionResponse>> ListAsync(ParkingLotId lotId, int page, int perPage,
    CancellationToken cancellationToken);
}

public record ActiveSessionResponse(
  Guid Id,
  string Plate,
  Guid? DriverId,
  string? DriverName,
  SessionPool Pool,
  Guid? SpaceId,
  string? SpaceLabel,
  DateTimeOffset EntryTime);
