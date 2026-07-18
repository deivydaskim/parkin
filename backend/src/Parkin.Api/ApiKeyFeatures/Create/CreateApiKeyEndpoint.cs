using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ApiKeyAggregate;
using Parkin.Api.Extensions;

namespace Parkin.Api.ApiKeyFeatures.Create;

public sealed class CreateApiKeyRequest
{
  public const string Route = "/api-keys";

  public string Name { get; init; } = string.Empty;
}

public record CreateApiKeyResponse(
  Guid Id,
  string Name,
  string Prefix,
  ApiKeyStatus Status,
  DateTimeOffset CreatedAt,
  DateTimeOffset? RevokedAt,
  string Key);

public class CreateApiKeyEndpoint(IMediator mediator)
  : Endpoint<CreateApiKeyRequest, Results<Created<CreateApiKeyResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(CreateApiKeyRequest.Route);
    Roles(AccessPolicies.AdminOnly);

    Summary(s =>
    {
      s.Summary = "Generate an API key";
      s.Description = "Generates a new API key for machine clients (e.g. the gate ingestion endpoint). The raw key is returned once and only as a hash thereafter.";
      s.Responses[201] = "API key created successfully; response includes the raw key, shown only this once";
      s.Responses[400] = "Invalid request data";
    });

    Tags("ApiKeys");

    Description(builder => builder
      .Accepts<CreateApiKeyRequest>()
      .Produces<CreateApiKeyResponse>(201, "application/json")
      .ProducesProblem(400));
  }

  public override async Task<Results<Created<CreateApiKeyResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(CreateApiKeyRequest request, CancellationToken cancellationToken)
  {
    var actorIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var actorId = actorIdClaim is null ? (Guid?)null : Guid.Parse(actorIdClaim);
    var command = new CreateApiKeyCommand(request.Name, actorId);

    var result = await mediator.Send(command, cancellationToken);

    return result.ToCreatedResult(
      created => $"/api-keys/{created.ApiKey.Id.Value}",
      created => new CreateApiKeyResponse(
        created.ApiKey.Id.Value,
        created.ApiKey.Name,
        created.ApiKey.Prefix,
        created.ApiKey.Status,
        created.ApiKey.CreatedAt,
        created.ApiKey.RevokedAt,
        created.RawKey));
  }
}

public sealed class CreateApiKeyValidator : Validator<CreateApiKeyRequest>
{
  public CreateApiKeyValidator()
  {
    RuleFor(x => x.Name)
      .NotEmpty().WithMessage("Name is required")
      .MaximumLength(200).WithMessage("Name must not exceed 200 characters");
  }
}
