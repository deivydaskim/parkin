using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Extensions;

namespace Parkin.Api.LotLayoutFeatures.Get;

public sealed class GetLotLayoutRequest
{
  public const string Route = "/lots/{LotId}/layout";
  public Guid LotId { get; init; }
}

public class GetLotLayoutEndpoint(IMediator mediator)
  : Endpoint<GetLotLayoutRequest, Results<Ok<LotLayoutViewRecord>, NotFound, ProblemHttpResult>>
{
  public override void Configure()
  {
    Get(GetLotLayoutRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Get the spatial layout of a parking lot";
      s.Description = "Returns the lot footprint and every space (active and inactive, placed and unplaced) with its " +
        "zone, placement and current reservation assignee. Configuration only; it never reports per-bay occupancy. " +
        "Archived lots are readable.";
      s.Responses[200] = "Layout returned successfully";
      s.Responses[404] = "Lot with specified ID not found";
    });

    Tags("Layout");

    Description(builder => builder
      .Accepts<GetLotLayoutRequest>()
      .Produces<LotLayoutViewRecord>(200, "application/json")
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<LotLayoutViewRecord>, NotFound, ProblemHttpResult>>
    ExecuteAsync(GetLotLayoutRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new GetLotLayoutQuery(ParkingLotId.From(request.LotId)), cancellationToken);

    return result.ToGetByIdResult(LotLayoutMapping.ToRecord);
  }
}

public sealed class GetLotLayoutValidator : Validator<GetLotLayoutRequest>
{
  public GetLotLayoutValidator()
  {
    RuleFor(x => x.LotId)
      .NotEmpty()
      .WithMessage("Lot ID is required");
  }
}
