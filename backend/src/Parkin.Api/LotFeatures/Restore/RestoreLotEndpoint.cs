using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Extensions;

namespace Parkin.Api.LotFeatures.Restore;

public sealed class RestoreLotRequest
{
  public const string Route = "/lots/{LotId}/restore";
  public Guid LotId { get; init; }
}

public class RestoreLotEndpoint(IMediator mediator)
  : Endpoint<RestoreLotRequest, Results<Ok<LotRecord>, NotFound, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(RestoreLotRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Restore an archived parking lot";
      s.Description = "Restores an archived lot back to active. Fails if another lot already holds the same name — rename or archive that one first.";
      s.Responses[200] = "Lot restored successfully";
      s.Responses[404] = "Lot with specified ID not found";
      s.Responses[400] = "A lot with this name already exists";
    });

    Tags("Lots");

    Description(builder => builder
      .Accepts<RestoreLotRequest>()
      .Produces<LotRecord>(200, "application/json")
      .ProducesProblem(404)
      .ProducesProblem(400));
  }

  public override async Task<Results<Ok<LotRecord>, NotFound, ProblemHttpResult>>
    ExecuteAsync(RestoreLotRequest request, CancellationToken cancellationToken)
  {
    var actorIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var actorId = actorIdClaim is null ? (Guid?)null : Guid.Parse(actorIdClaim);
    var result = await mediator.Send(new RestoreLotCommand(ParkingLotId.From(request.LotId), actorId), cancellationToken);

    return result.ToUpdateResult(LotEnumMapping.ToRecord);
  }
}

public sealed class RestoreLotValidator : Validator<RestoreLotRequest>
{
  public RestoreLotValidator()
  {
    RuleFor(x => x.LotId)
      .NotEmpty()
      .WithMessage("Lot ID is required");
  }
}
