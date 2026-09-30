namespace Parkin.Api.Features.Drivers.List;

public record DriverListResponse(IReadOnlyList<DriverResponse> Items, int Page, int PerPage, int TotalCount, int TotalPages)
  : PagedResult<DriverResponse>(Items, Page, PerPage, TotalCount, TotalPages)
{
  public static DriverListResponse From(PagedResult<DriverResponse> page)
    => new(page.Items, page.Page, page.PerPage, page.TotalCount, page.TotalPages);
}
