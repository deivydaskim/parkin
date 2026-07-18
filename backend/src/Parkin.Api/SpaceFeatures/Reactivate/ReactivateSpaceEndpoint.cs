using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Extensions;

namespace Parkin.Api.SpaceFeatures.Reactivate;

public sealed class ReactivateSpaceRequest
{
  public const string Route = "/spaces/{SpaceId}/reactivate";
  public Guid SpaceId { get; init; }
}

public class ReactivateSpaceEndpoint(IMediator mediator)
  : Endpoint<ReactivateSpaceRequest, Results<Ok<SpaceRecord>, NotFound, ProblemHttpResult>>
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
      .Produces<SpaceRecord>(200, "application/json")
      .ProducesProblem(404)
      .ProducesProblem(400));
  }

  public override async Task<Results<Ok<SpaceRecord>, NotFound, ProblemHttpResult>>
    ExecuteAsync(ReactivateSpaceRequest request, CancellationToken cancellationToken)
  {
    var actorIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var actorId = actorIdClaim is null ? (Guid?)null : Guid.Parse(actorIdClaim);
    var result = await mediator.Send(new ReactivateSpaceCommand(ParkingSpaceId.From(request.SpaceId), actorId), cancellationToken);

    return result.ToUpdateResult(SpaceEnumMapping.ToRecord);
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
