using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;

namespace Parkin.Api.ApiKeyFeatures.List;

public class ListApiKeysEndpoint(IMediator mediator)
  : EndpointWithoutRequest<Ok<IReadOnlyList<ApiKeyRecord>>>
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
      .Produces<IReadOnlyList<ApiKeyRecord>>(200, "application/json"));
  }

  public override async Task<Ok<IReadOnlyList<ApiKeyRecord>>> ExecuteAsync(CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new ListApiKeysQuery(), cancellationToken);
    IReadOnlyList<ApiKeyRecord> response = result.Value.Select(ApiKeyMapping.ToRecord).ToList();
    return TypedResults.Ok(response);
  }
}
