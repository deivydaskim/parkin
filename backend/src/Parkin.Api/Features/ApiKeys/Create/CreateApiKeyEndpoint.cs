using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ApiKeyAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.ApiKeys.Create;

public sealed class CreateApiKeyRequest
{
  public const string Route = "/api-keys";

  public string Name { get; init; } = string.Empty;
  public Guid LotId { get; init; }
}

public record CreateApiKeyResponse(
  Guid Id,
  string Name,
  Guid LotId,
  string LotName,
  string Prefix,
  ApiKeyStatus Status,
  DateTimeOffset CreatedAt,
  DateTimeOffset? RevokedAt,
  string Key)
{
  public static CreateApiKeyResponse From(ApiKey apiKey, string lotName, string rawKey)
    => new(apiKey.Id.Value, apiKey.Name, apiKey.LotId.Value, lotName, apiKey.Prefix, apiKey.Status, apiKey.CreatedAt,
      apiKey.RevokedAt, rawKey);
}

public class CreateApiKeyEndpoint(IMediator mediator, ICurrentUser currentUser)
  : Endpoint<CreateApiKeyRequest, Results<Created<CreateApiKeyResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(CreateApiKeyRequest.Route);
    Roles(AccessPolicies.AdminOnly);

    Summary(s =>
    {
      s.Summary = "Generate an API key";
      s.Description = "Generates a new API key for machine clients (e.g. the gate ingestion endpoint), bound to one active parking lot. The raw key is returned once and only as a hash thereafter.";
      s.Responses[201] = "API key created successfully; response includes the raw key, shown only this once";
      s.Responses[400] = "Invalid request data, or the lot is archived";
      s.Responses[404] = "Lot not found";
    });

    Tags("ApiKeys");

    Description(builder => builder
      .Accepts<CreateApiKeyRequest>()
      .Produces<CreateApiKeyResponse>(201, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(404));
  }

  public override async Task<Results<Created<CreateApiKeyResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(CreateApiKeyRequest request, CancellationToken cancellationToken)
  {
    var command = new CreateApiKeyCommand(request.Name, ParkingLotId.From(request.LotId), currentUser.Id);
    var result = await mediator.Send(command, cancellationToken);
    return result.ToCreatedResult(created => $"/api-keys/{created.Id}", created => created);
  }
}

public sealed class CreateApiKeyValidator : Validator<CreateApiKeyRequest>
{
  public CreateApiKeyValidator()
  {
    RuleFor(x => x.Name)
      .NotEmpty().WithMessage("Name is required")
      .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

    RuleFor(x => x.LotId)
      .NotEmpty().WithMessage("Lot ID is required");
  }
}
