using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;

namespace Parkin.Api.Web;

public static class PaginationLinkHeader
{
  public const string PageParameter = "page";
  public const string PerPageParameter = "per_page";

  public static void AppendPaginationLinks<T>(this HttpContext context, PagedResult<T> page)
  {
    var links = new List<string>();
    if (page.Page > 1)
    {
      links.Add(Link(context.Request, "first", 1, page.PerPage));
      links.Add(Link(context.Request, "prev", page.Page - 1, page.PerPage));
    }

    if (page.Page < page.TotalPages)
    {
      links.Add(Link(context.Request, "next", page.Page + 1, page.PerPage));
      links.Add(Link(context.Request, "last", page.TotalPages, page.PerPage));
    }

    if (links.Count > 0)
    {
      context.Response.Headers.Link = string.Join(", ", links);
    }
  }

  private static string Link(HttpRequest request, string rel, int pageNumber, int perPage)
  {
    var query = QueryHelpers.ParseQuery(request.QueryString.Value)
      .Where(pair => pair.Key is not (PageParameter or PerPageParameter))
      .SelectMany(pair => pair.Value.Select(value => new KeyValuePair<string, string?>(pair.Key, value)))
      .Append(new(PageParameter, pageNumber.ToString()))
      .Append(new(PerPageParameter, perPage.ToString()));

    var baseUrl = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase, request.Path);
    return $"<{QueryHelpers.AddQueryString(baseUrl, query)}>; rel=\"{rel}\"";
  }
}
