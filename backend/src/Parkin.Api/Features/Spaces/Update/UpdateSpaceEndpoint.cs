using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Spaces.Update;

public sealed class UpdateSpaceRequest
{
  public const string Route = "/spaces/{SpaceId}";

  public Guid SpaceId { get; init; }
  public string? Label { get; init; }
  public SpaceType? Type { get; init; }
  public string? Zone { get; init; }
  public SpacePlacementRequest? Placement { get; init; }
  public bool ClearPlacement { get; init; }
}

public class UpdateSpaceEndpoint(IMediator mediator, ICurrentUser currentUser)
  : Endpoint<UpdateSpaceRequest, Results<Ok<SpaceResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Patch(UpdateSpaceRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Update a parking space";
      s.Description = "Partially updates a parking space. Only the fields present in the request body are applied. " +
        "An empty zone clears it; clearPlacement=true un-places the space.";
      s.Responses[200] = "Space updated successfully";
      s.Responses[404] = "Space with specified ID not found";
      s.Responses[400] = "Invalid request data, a duplicate label, or a placement outside the lot layout";
    });

    Tags("Spaces");

    Description(builder => builder
      .Accepts<UpdateSpaceRequest>()
      .Produces<SpaceResponse>(200, "application/json")
      .ProducesProblem(404)
      .ProducesProblem(400));
  }

  public override async Task<Results<Ok<SpaceResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(UpdateSpaceRequest request, CancellationToken cancellationToken)
  {
    var update = new SpaceUpdate(request.Label, request.Type, request.Zone, request.Placement?.ToValue().Value,
      request.ClearPlacement);
    var command = new UpdateSpaceCommand(ParkingSpaceId.From(request.SpaceId), update, currentUser.Id);

    var result = await mediator.Send(command, cancellationToken);

    return result.ToOkResult(space => space);
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
      .NotEmpty()
      .WithMessage("Label cannot be blank")
      .MaximumLength(ParkingSpace.LabelMaxLength)
      .WithMessage($"Label must not exceed {ParkingSpace.LabelMaxLength} characters")
      .When(x => x.Label is not null);

    RuleFor(x => x.Placement!)
      .SetValidator(new SpacePlacementRequestValidator())
      .When(x => x.Placement is not null);

    RuleFor(x => x.ClearPlacement)
      .Equal(false)
      .WithMessage("Send either placement or clearPlacement, not both")
      .When(x => x.Placement is not null);
  }
}
