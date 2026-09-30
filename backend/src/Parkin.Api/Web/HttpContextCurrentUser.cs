using System.Security.Claims;

namespace Parkin.Api.Web;

public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
  public Guid? Id =>
    Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
      ? id
      : null;

  public Guid RequiredId => Id ?? throw new InvalidOperationException("No authenticated principal on this request.");
}
