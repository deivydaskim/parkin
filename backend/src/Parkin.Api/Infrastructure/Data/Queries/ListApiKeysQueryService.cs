using Microsoft.EntityFrameworkCore;
using Parkin.Api.Features.ApiKeys;
using Parkin.Api.Features.ApiKeys.List;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ListApiKeysQueryService(AppDbContext db) : IListApiKeysQueryService
{
  public async Task<IReadOnlyList<ApiKeyResponse>> ListAsync(CancellationToken cancellationToken)
  {
    var rows = await db.ApiKeys
      .Join(db.ParkingLots, key => key.LotId, lot => lot.Id, (key, lot) => new { Key = key, LotName = lot.Name })
      .OrderByDescending(row => row.Key.CreatedAt)
      .AsNoTracking()
      .ToListAsync(cancellationToken);

    return rows.Select(row => ApiKeyResponse.From(row.Key, row.LotName)).ToList();
  }
}
