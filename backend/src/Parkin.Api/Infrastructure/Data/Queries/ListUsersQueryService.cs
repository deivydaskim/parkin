using Microsoft.EntityFrameworkCore;
using Parkin.Api.Features.Users;
using Parkin.Api.Features.Users.List;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ListUsersQueryService(AppDbContext db) : IListUsersQueryService
{
  public async Task<PagedResult<UserResponse>> ListAsync(int page, int perPage, CancellationToken cancellationToken)
  {
    var items = await db.Users
      .OrderBy(u => u.Email)
      .Skip((page - 1) * perPage)
      .Take(perPage)
      .Select(u => new UserResponse(
        u.Id,
        u.Email!,
        u.DisplayName,
        (from ur in db.UserRoles
         join r in db.Roles on ur.RoleId equals r.Id
         where ur.UserId == u.Id
         select r.Name!).FirstOrDefault() ?? string.Empty,
        u.Status.ToString()))
      .AsNoTracking()
      .ToListAsync(cancellationToken);

    var totalCount = await db.Users.CountAsync(cancellationToken);
    var totalPages = (int)Math.Ceiling(totalCount / (double)perPage);

    return new PagedResult<UserResponse>(items, page, perPage, totalCount, totalPages);
  }
}
