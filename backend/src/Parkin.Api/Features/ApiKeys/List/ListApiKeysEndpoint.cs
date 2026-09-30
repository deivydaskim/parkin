using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Web;

namespace Parkin.Api.Features.ApiKeys.List;

public class ListApiKeysEndpoint(IMediator mediator)
  : EndpointWithoutRequest<Results<Ok<IReadOnlyList<ApiKeyResponse>>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Get("/api-keys");
    Roles(AccessPolicies.AdminOnly);

    Summary(s =>
    {
      s.Summary = "List API keys";
      s.Description = "Lists all API keys, newest first. Never includes the raw key or its hash — only the display prefix.";
      s.Responses[200] = "API keys returned successfully";
    });

    Tags("ApiKeys");

    Description(builder => builder
      .Produces<IReadOnlyList<ApiKeyResponse>>(200, "application/json"));
  }

  public override async Task<Results<Ok<IReadOnlyList<ApiKeyResponse>>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new ListApiKeysQuery(), cancellationToken);
    return result.ToOkResult(keys => keys);
  }
}
