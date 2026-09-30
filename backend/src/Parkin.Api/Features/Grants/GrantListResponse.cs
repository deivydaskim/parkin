namespace Parkin.Api.Features.Grants;

public record GrantListResponse(IReadOnlyList<GrantResponse> Items, int Page, int PerPage, int TotalCount, int TotalPages)
  : PagedResult<GrantResponse>(Items, Page, PerPage, TotalCount, TotalPages)
{
  public static GrantListResponse From(PagedResult<GrantResponse> page)
    => new(page.Items, page.Page, page.PerPage, page.TotalCount, page.TotalPages);
}
