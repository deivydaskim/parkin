using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Occupancy.ListLotOccupancy;

public class ListLotOccupancyEndpoint(IMediator mediator)
  : EndpointWithoutRequest<Results<Ok<IReadOnlyList<LotOccupancyResponse>>, ValidationProblem, ProblemHttpResult>>
{
  public const string Route = "/occupancy";

  public override void Configure()
  {
    Get(Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Get live occupancy for every active parking lot";
      s.Description = "Returns the same derived occupancy as /lots/{lotId}/occupancy for each active lot, " +
        "ordered by lot name and including the lot name, computed in one batch for dashboards.";
      s.Responses[200] = "Occupancy computed and returned successfully";
    });

    Tags("Occupancy");

    Description(builder => builder
      .Produces<IReadOnlyList<LotOccupancyResponse>>(200, "application/json"));
  }

  public override async Task<Results<Ok<IReadOnlyList<LotOccupancyResponse>>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new ListLotOccupancyQuery(), cancellationToken);

    return result.ToOkResult(occupancies => occupancies);
  }
}
