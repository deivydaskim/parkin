using Microsoft.EntityFrameworkCore;

namespace Parkin.Api.Infrastructure.Data.Queries;

internal static class PagingExtensions
{
  public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> query, int page, int perPage,
    CancellationToken cancellationToken)
  {
    var items = await query
      .Skip((page - 1) * perPage)
      .Take(perPage)
      .ToListAsync(cancellationToken);

    var totalCount = await query.CountAsync(cancellationToken);
    var totalPages = (int)Math.Ceiling(totalCount / (double)perPage);

    return new PagedResult<T>(items, page, perPage, totalCount, totalPages);
  }
}
