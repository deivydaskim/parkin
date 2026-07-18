using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ApiKeyAggregate;
using Parkin.Api.Extensions;

namespace Parkin.Api.ApiKeyFeatures.Revoke;

public sealed class RevokeApiKeyRequest
{
  public const string Route = "/api-keys/{ApiKeyId}/revoke";

  public Guid ApiKeyId { get; init; }
}

public class RevokeApiKeyEndpoint(IMediator mediator)
  : Endpoint<RevokeApiKeyRequest, Results<Ok<ApiKeyRecord>, NotFound, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(RevokeApiKeyRequest.Route);
    Roles(AccessPolicies.AdminOnly);

    Summary(s =>
    {
      s.Summary = "Revoke an API key";
      s.Description = "Revokes an API key immediately; any request presenting it thereafter is rejected.";
      s.Responses[200] = "API key revoked successfully";
      s.Responses[404] = "API key with specified ID not found";
    });

    Tags("ApiKeys");

    Description(builder => builder
      .Accepts<RevokeApiKeyRequest>()
      .Produces<ApiKeyRecord>(200, "application/json")
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<ApiKeyRecord>, NotFound, ProblemHttpResult>>
    ExecuteAsync(RevokeApiKeyRequest request, CancellationToken cancellationToken)
  {
    var actorIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var actorId = actorIdClaim is null ? (Guid?)null : Guid.Parse(actorIdClaim);
    var result = await mediator.Send(new RevokeApiKeyCommand(ApiKeyId.From(request.ApiKeyId), actorId), cancellationToken);

    return result.ToUpdateResult(ApiKeyMapping.ToRecord);
  }
}

public sealed class RevokeApiKeyValidator : Validator<RevokeApiKeyRequest>
{
  public RevokeApiKeyValidator()
  {
    RuleFor(x => x.ApiKeyId)
      .NotEmpty()
      .WithMessage("API key ID is required");
  }
}
