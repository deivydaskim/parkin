using Microsoft.EntityFrameworkCore;
using Parkin.Api.UserFeatures;
using Parkin.Api.UserFeatures.List;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ListUsersQueryService(AppDbContext db) : IListUsersQueryService
{
  private readonly AppDbContext _db = db;

  public async Task<PagedResult<UserRecord>> ListAsync(int page, int perPage)
  {
    var query = _db.Users.AsQueryable();

    var items = await query
      .OrderBy(u => u.Email)
      .Skip((page - 1) * perPage)
      .Take(perPage)
      .Select(u => new UserRecord(
        u.Id,
        u.Email!,
        u.DisplayName,
        (from ur in _db.UserRoles
         join r in _db.Roles on ur.RoleId equals r.Id
         where ur.UserId == u.Id
         select r.Name!).FirstOrDefault() ?? string.Empty,
        u.Status.ToString()))
      .AsNoTracking()
      .ToListAsync();

    int totalCount = await query.CountAsync();
    int totalPages = (int)Math.Ceiling(totalCount / (double)perPage);

    return new PagedResult<UserRecord>(items, page, perPage, totalCount, totalPages);
  }
}
