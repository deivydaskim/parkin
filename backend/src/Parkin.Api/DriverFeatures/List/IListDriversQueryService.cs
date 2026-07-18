namespace Parkin.Api.DriverFeatures.List;

public interface IListDriversQueryService
{
  Task<PagedResult<DriverDto>> ListAsync(int page, int perPage, DriverStatusFilter? status);
}
