using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Lots.Restore;

public sealed class RestoreLotRequest
{
  public const string Route = "/lots/{LotId}/restore";
  public Guid LotId { get; init; }
}

public class RestoreLotEndpoint(IMediator mediator, ICurrentUser currentUser)
  : Endpoint<RestoreLotRequest, Results<Ok<LotResponse>, ValidationProblem, ProblemHttpResult>>
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
      .Produces<LotResponse>(200, "application/json")
      .ProducesProblem(404)
      .ProducesProblem(400));
  }

  public override async Task<Results<Ok<LotResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(RestoreLotRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
      new RestoreLotCommand(ParkingLotId.From(request.LotId), currentUser.Id), cancellationToken);

    return result.ToOkResult(lot => lot);
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
