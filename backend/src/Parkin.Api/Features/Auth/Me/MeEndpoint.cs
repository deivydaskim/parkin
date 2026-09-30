using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Auth.Me;

public class MeEndpoint(IMediator mediator, ICurrentUser currentUser) :
  EndpointWithoutRequest<Results<Ok<CurrentUserResponse>, UnauthorizedHttpResult>>
{
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
    var result = await mediator.Send(new GetCurrentUserQuery(currentUser.Id), cancellationToken);
    return result.IsSuccess ? TypedResults.Ok(result.Value) : TypedResults.Unauthorized();
  }
}
