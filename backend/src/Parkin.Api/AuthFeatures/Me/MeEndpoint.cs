using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Parkin.Api.Infrastructure.Identity;

namespace Parkin.Api.AuthFeatures.Me;

public class MeEndpoint(UserManager<ApplicationUser> userManager) :
  EndpointWithoutRequest<Results<Ok<CurrentUserResponse>, UnauthorizedHttpResult>>
{
  private readonly UserManager<ApplicationUser> _userManager = userManager;

  public override void Configure()
  {
    Get("/auth/me");

    Summary(s =>
    {
      s.Summary = "Current authenticated staff member";
      s.Description = "Returns the signed-in user's identity and roles; used to hydrate the SPA.";
      s.Responses[200] = "Current user";
      s.Responses[401] = "Not authenticated";
    });

    Tags("Auth");
  }

  public override async Task<Results<Ok<CurrentUserResponse>, UnauthorizedHttpResult>>
    ExecuteAsync(CancellationToken cancellationToken)
  {
    var user = await _userManager.GetUserAsync(User);
    if (user is null || user.Status == UserStatus.Disabled)
    {
      return TypedResults.Unauthorized();
    }

    var response = await CurrentUserResponseFactory.BuildAsync(_userManager, user);
    return TypedResults.Ok(response);
  }
}
