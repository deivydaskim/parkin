using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Lots.GetById;

public sealed class GetLotRequest
{
  public const string Route = "/lots/{LotId}";
  public Guid LotId { get; init; }
}

public class GetLotEndpoint(IMediator mediator)
  : Endpoint<GetLotRequest, Results<Ok<LotResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Get(GetLotRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Get a parking lot by ID";
      s.Description = "Retrieves a specific parking lot by its unique identifier, regardless of its status (active or archived).";
      s.Responses[200] = "Lot found and returned successfully";
      s.Responses[404] = "Lot with specified ID not found";
    });

    Tags("Lots");

    Description(builder => builder
      .Accepts<GetLotRequest>()
      .Produces<LotResponse>(200, "application/json")
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<LotResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(GetLotRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new GetLotQuery(ParkingLotId.From(request.LotId)), cancellationToken);

    return result.ToOkResult(lot => lot);
  }
}

public sealed class GetLotValidator : Validator<GetLotRequest>
{
  public GetLotValidator()
  {
    RuleFor(x => x.LotId)
      .NotEmpty()
      .WithMessage("Lot ID is required");
  }
}
