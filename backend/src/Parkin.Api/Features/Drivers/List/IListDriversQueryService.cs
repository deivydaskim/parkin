namespace Parkin.Api.Features.Drivers.List;

public interface IListDriversQueryService
{
  Task<PagedResult<DriverResponse>> ListAsync(int page, int perPage, DriverStatusFilter? status, string? search,
    CancellationToken cancellationToken);
}
