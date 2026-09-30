using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Domain.StaffUsers;

namespace Parkin.Api.Features.Auth.Logout;

public class LogoutEndpoint(IStaffAuthService staffAuth) : EndpointWithoutRequest<NoContent>
{
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
    await staffAuth.SignOutAsync();
    return TypedResults.NoContent();
  }
}
