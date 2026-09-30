using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ApiKeyAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.ApiKeys.Revoke;

public sealed class RevokeApiKeyRequest
{
  public const string Route = "/api-keys/{ApiKeyId}/revoke";

  public Guid ApiKeyId { get; init; }
}

public class RevokeApiKeyEndpoint(IMediator mediator, ICurrentUser currentUser)
  : Endpoint<RevokeApiKeyRequest, Results<Ok<ApiKeyResponse>, ValidationProblem, ProblemHttpResult>>
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
      s.Responses[409] = "API key is already revoked";
    });

    Tags("ApiKeys");

    Description(builder => builder
      .Accepts<RevokeApiKeyRequest>()
      .Produces<ApiKeyResponse>(200, "application/json")
      .ProducesProblem(404)
      .ProducesProblem(409));
  }

  public override async Task<Results<Ok<ApiKeyResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(RevokeApiKeyRequest request, CancellationToken cancellationToken)
  {
    var command = new RevokeApiKeyCommand(ApiKeyId.From(request.ApiKeyId), currentUser.Id);
    var result = await mediator.Send(command, cancellationToken);
    return result.ToOkResult(apiKey => apiKey);
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
