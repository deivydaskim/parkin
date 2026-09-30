using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Spaces.Reactivate;

public sealed class ReactivateSpaceRequest
{
  public const string Route = "/spaces/{SpaceId}/reactivate";
  public Guid SpaceId { get; init; }
}

public class ReactivateSpaceEndpoint(IMediator mediator, ICurrentUser currentUser)
  : Endpoint<ReactivateSpaceRequest, Results<Ok<SpaceResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(ReactivateSpaceRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Reactivate a parking space";
      s.Description = "Reactivates a deactivated parking space. Fails if another space in the lot already holds the same label — rename or deactivate that one first.";
      s.Responses[200] = "Space reactivated successfully";
      s.Responses[404] = "Space with specified ID not found";
      s.Responses[400] = "A space with this label already exists in the lot";
    });

    Tags("Spaces");

    Description(builder => builder
      .Accepts<ReactivateSpaceRequest>()
      .Produces<SpaceResponse>(200, "application/json")
      .ProducesProblem(404)
      .ProducesProblem(400));
  }

  public override async Task<Results<Ok<SpaceResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(ReactivateSpaceRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
      new ReactivateSpaceCommand(ParkingSpaceId.From(request.SpaceId), currentUser.Id), cancellationToken);

    return result.ToOkResult(space => space);
  }
}

public sealed class ReactivateSpaceValidator : Validator<ReactivateSpaceRequest>
{
  public ReactivateSpaceValidator()
  {
    RuleFor(x => x.SpaceId)
      .NotEmpty()
      .WithMessage("Space ID is required");
  }
}
