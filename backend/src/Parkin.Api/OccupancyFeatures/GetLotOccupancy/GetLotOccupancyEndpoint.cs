using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Extensions;

namespace Parkin.Api.OccupancyFeatures.GetLotOccupancy;

public sealed class GetLotOccupancyRequest
{
  public const string Route = "/lots/{LotId}/occupancy";
  public Guid LotId { get; init; }
}

public class GetLotOccupancyEndpoint(IMediator mediator)
  : Endpoint<GetLotOccupancyRequest, Results<Ok<LotOccupancyRecord>, NotFound, ProblemHttpResult>>
{
  public override void Configure()
  {
    Get(GetLotOccupancyRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Get live occupancy for a parking lot";
      s.Description = "Returns the lot's derived occupancy: general capacity (active GENERAL spaces), " +
        "general used (active GENERAL sessions), general free (floored at 0), reserved space and " +
        "reserved-occupied counts, and an over-capacity flag. Occupancy is lot-level and counted at " +
        "the gate, not per-space sensed. Archived lots are readable so lingering sessions stay visible.";
      s.Responses[200] = "Occupancy computed and returned successfully";
      s.Responses[404] = "Lot with specified ID not found";
    });

    Tags("Occupancy");

    Description(builder => builder
      .Accepts<GetLotOccupancyRequest>()
      .Produces<LotOccupancyRecord>(200, "application/json")
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<LotOccupancyRecord>, NotFound, ProblemHttpResult>>
    ExecuteAsync(GetLotOccupancyRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
      new GetLotOccupancyQuery(ParkingLotId.From(request.LotId)), cancellationToken);

    return result.ToGetByIdResult(OccupancyMapping.ToRecord);
  }
}

public sealed class GetLotOccupancyValidator : Validator<GetLotOccupancyRequest>
{
  public GetLotOccupancyValidator()
  {
    RuleFor(x => x.LotId)
      .NotEmpty()
      .WithMessage("Lot ID is required");
  }
}
