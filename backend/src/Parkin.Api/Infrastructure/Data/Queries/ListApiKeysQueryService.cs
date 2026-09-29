using Microsoft.EntityFrameworkCore;
using Parkin.Api.Features.ApiKeys;
using Parkin.Api.Features.ApiKeys.List;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ListApiKeysQueryService(AppDbContext db) : IListApiKeysQueryService
{
  public async Task<IReadOnlyList<ApiKeyDto>> ListAsync(CancellationToken cancellationToken)
  {
    return await db.ApiKeys
      .OrderByDescending(k => k.CreatedAt)
      .Select(k => new ApiKeyDto(k.Id, k.Name, k.Prefix, k.Status, k.CreatedAt, k.RevokedAt))
      .AsNoTracking()
      .ToListAsync(cancellationToken);
  }
}
