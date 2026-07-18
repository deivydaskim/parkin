using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Extensions;

namespace Parkin.Api.SpaceFeatures.Update;

public sealed class UpdateSpaceRequest
{
  public const string Route = "/spaces/{SpaceId}";

  public Guid SpaceId { get; init; }
  public string? Label { get; init; }
  public SpaceType? Type { get; init; }
}

public class UpdateSpaceEndpoint(IMediator mediator)
  : Endpoint<UpdateSpaceRequest, Results<Ok<SpaceRecord>, NotFound, ProblemHttpResult>>
{
  public override void Configure()
  {
    Patch(UpdateSpaceRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Update a parking space";
      s.Description = "Partially updates a parking space. Only the fields present in the request body are applied.";
      s.Responses[200] = "Space updated successfully";
      s.Responses[404] = "Space with specified ID not found";
      s.Responses[400] = "Invalid request data, or a space with this label already exists in the lot";
    });

    Tags("Spaces");

    Description(builder => builder
      .Accepts<UpdateSpaceRequest>()
      .Produces<SpaceRecord>(200, "application/json")
      .ProducesProblem(404)
      .ProducesProblem(400));
  }

  public override async Task<Results<Ok<SpaceRecord>, NotFound, ProblemHttpResult>>
    ExecuteAsync(UpdateSpaceRequest request, CancellationToken cancellationToken)
  {
    var actorIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var actorId = actorIdClaim is null ? (Guid?)null : Guid.Parse(actorIdClaim);
    var command = new UpdateSpaceCommand(ParkingSpaceId.From(request.SpaceId), request.Label, request.Type, actorId);

    var result = await mediator.Send(command, cancellationToken);

    return result.ToUpdateResult(SpaceEnumMapping.ToRecord);
  }
}

public sealed class UpdateSpaceValidator : Validator<UpdateSpaceRequest>
{
  public UpdateSpaceValidator()
  {
    RuleFor(x => x.SpaceId)
      .NotEmpty()
      .WithMessage("Space ID is required");

    RuleFor(x => x.Label)
      .MaximumLength(100)
      .WithMessage("Label must not exceed 100 characters")
      .When(x => x.Label is not null);

    RuleFor(x => x.Label)
      .NotEmpty()
      .WithMessage("Label cannot be blank")
      .When(x => x.Label is not null);
  }
}
