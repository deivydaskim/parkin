namespace Parkin.Api.Features.Plates.List;

public record PlateListResponse(IReadOnlyList<PlateResponse> Items, int Page, int PerPage, int TotalCount, int TotalPages)
  : PagedResult<PlateResponse>(Items, Page, PerPage, TotalCount, TotalPages)
{
  public static PlateListResponse From(PagedResult<PlateResponse> page)
    => new(page.Items, page.Page, page.PerPage, page.TotalCount, page.TotalPages);
}
