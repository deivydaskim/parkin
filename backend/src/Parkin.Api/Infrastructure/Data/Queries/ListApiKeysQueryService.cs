using Microsoft.EntityFrameworkCore;
using Parkin.Api.Features.ApiKeys;
using Parkin.Api.Features.ApiKeys.List;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ListApiKeysQueryService(AppDbContext db) : IListApiKeysQueryService
{
  public async Task<IReadOnlyList<ApiKeyResponse>> ListAsync(CancellationToken cancellationToken)
  {
    var keys = await db.ApiKeys
      .OrderByDescending(k => k.CreatedAt)
      .AsNoTracking()
      .ToListAsync(cancellationToken);

    return keys.Select(ApiKeyResponse.From).ToList();
  }
}
