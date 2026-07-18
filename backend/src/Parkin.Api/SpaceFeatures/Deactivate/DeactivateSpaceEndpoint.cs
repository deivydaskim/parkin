using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Extensions;

namespace Parkin.Api.SpaceFeatures.Deactivate;

public sealed class DeactivateSpaceRequest
{
  public const string Route = "/spaces/{SpaceId}/deactivate";
  public Guid SpaceId { get; init; }
}

public class DeactivateSpaceEndpoint(IMediator mediator)
  : Endpoint<DeactivateSpaceRequest, Results<Ok<SpaceRecord>, NotFound, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(DeactivateSpaceRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Deactivate a parking space";
      s.Description = "Soft-deactivates a parking space. Drops it from capacity and the live view but preserves history. Blocked for a Reserved space with an active reservation.";
      s.Responses[200] = "Space deactivated successfully";
      s.Responses[404] = "Space with specified ID not found";
      s.Responses[400] = "Space has an active reservation and cannot be deactivated";
    });

    Tags("Spaces");

    Description(builder => builder
      .Accepts<DeactivateSpaceRequest>()
      .Produces<SpaceRecord>(200, "application/json")
      .ProducesProblem(404)
      .ProducesProblem(400));
  }

  public override async Task<Results<Ok<SpaceRecord>, NotFound, ProblemHttpResult>>
    ExecuteAsync(DeactivateSpaceRequest request, CancellationToken cancellationToken)
  {
    var actorIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var actorId = actorIdClaim is null ? (Guid?)null : Guid.Parse(actorIdClaim);
    var result = await mediator.Send(new DeactivateSpaceCommand(ParkingSpaceId.From(request.SpaceId), actorId), cancellationToken);

    return result.ToUpdateResult(SpaceEnumMapping.ToRecord);
  }
}

public sealed class DeactivateSpaceValidator : Validator<DeactivateSpaceRequest>
{
  public DeactivateSpaceValidator()
  {
    RuleFor(x => x.SpaceId)
      .NotEmpty()
      .WithMessage("Space ID is required");
  }
}
