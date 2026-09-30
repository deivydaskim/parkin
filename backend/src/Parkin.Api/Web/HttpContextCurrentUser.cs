using System.Security.Claims;
using Parkin.Api.Authorization;

namespace Parkin.Api.Web;

public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
  public Guid? Id => ParseClaim(ClaimTypes.NameIdentifier);

  public Guid RequiredId => Id ?? throw new InvalidOperationException("No authenticated principal on this request.");

  public Guid RequiredLotId => ParseClaim(ApiKeyClaimTypes.LotId)
    ?? throw new InvalidOperationException("The authenticated principal is not bound to a parking lot.");

  private Guid? ParseClaim(string claimType) =>
    Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue(claimType), out var value) ? value : null;
}
