using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Parkin.Api.Infrastructure.Identity;

namespace Parkin.Api.AuthFeatures.Logout;

public class LogoutEndpoint(SignInManager<ApplicationUser> signInManager) :
  EndpointWithoutRequest<NoContent>
{
  private readonly SignInManager<ApplicationUser> _signInManager = signInManager;

  public override void Configure()
  {
    Post("/auth/logout");

    Summary(s =>
    {
      s.Summary = "Staff logout";
      s.Description = "Clears the session cookie.";
      s.Responses[204] = "Signed out";
      s.Responses[401] = "Not authenticated";
    });

    Tags("Auth");
  }

  public override async Task<NoContent> ExecuteAsync(CancellationToken cancellationToken)
  {
    await _signInManager.SignOutAsync();
    return TypedResults.NoContent();
  }
}
