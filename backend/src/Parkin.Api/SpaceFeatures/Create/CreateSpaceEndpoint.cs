using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Extensions;

namespace Parkin.Api.SpaceFeatures.Create;

public sealed class CreateSpaceRequest
{
  public const string Route = "/lots/{LotId}/spaces";

  public Guid LotId { get; init; }
  public string Label { get; init; } = string.Empty;
  public SpaceType Type { get; init; } = SpaceType.General;
}

public class CreateSpaceEndpoint(IMediator mediator)
  : Endpoint<CreateSpaceRequest, Results<Created<SpaceRecord>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(CreateSpaceRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Create a parking space";
      s.Description = "Creates a new parking space within a lot. Label must be unique within the lot.";
      s.Responses[201] = "Space created successfully";
      s.Responses[400] = "Invalid request data, or a space with this label already exists in the lot";
      s.Responses[404] = "Lot with specified ID not found";
    });

    Tags("Spaces");

    Description(builder => builder
      .Accepts<CreateSpaceRequest>()
      .Produces<SpaceRecord>(201, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(404));
  }

  public override async Task<Results<Created<SpaceRecord>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(CreateSpaceRequest request, CancellationToken cancellationToken)
  {
    var actorIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var actorId = actorIdClaim is null ? (Guid?)null : Guid.Parse(actorIdClaim);
    var command = new CreateSpaceCommand(ParkingLotId.From(request.LotId), request.Label, request.Type, actorId);

    var result = await mediator.Send(command, cancellationToken);

    return result.ToCreatedResult(
      space => $"/spaces/{space.Id.Value}",
      SpaceEnumMapping.ToRecord);
  }
}

public sealed class CreateSpaceValidator : Validator<CreateSpaceRequest>
{
  public CreateSpaceValidator()
  {
    RuleFor(x => x.LotId)
      .NotEmpty()
      .WithMessage("Lot ID is required");

    RuleFor(x => x.Label)
      .NotEmpty()
      .WithMessage("Label is required")
      .MaximumLength(100)
      .WithMessage("Label must not exceed 100 characters");
  }
}
