using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Parkin.Api.Infrastructure.Identity;

namespace Parkin.Api.Features.Users;

internal static class ClaimsPrincipalExtensions
{
  public static Guid? ActorId(this ClaimsPrincipal principal, UserManager<ApplicationUser> userManager)
  {
    var id = userManager.GetUserId(principal);
    return id is null ? null : Guid.Parse(id);
  }
}
