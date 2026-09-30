using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.Services;

namespace Parkin.Api.Features.AccessEvents.List;

public interface IListAccessEventsQueryService
{
  Task<PagedResult<AccessEventListItemResponse>> ListByLotAsync(ParkingLotId lotId, int page, int perPage,
    CancellationToken cancellationToken);
}

public record AccessEventListItemResponse(
  Guid Id,
  string Plate,
  Direction Direction,
  Decision Decision,
  DenyReason? Reason,
  SessionPool? Pool,
  string? ReservedSpaceLabel,
  EventSource Source,
  Guid? DriverId,
  string? DriverName,
  DateTimeOffset OccurredAt);
