using FastEndpoints;
using Parkin.Api.Authorization;

namespace Parkin.Api.OccupancyFeatures.ListLotOccupancy;

public class ListLotOccupancyEndpoint(IMediator mediator)
  : EndpointWithoutRequest<IReadOnlyList<LotOccupancyRecord>>
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
      .Produces<IReadOnlyList<LotOccupancyRecord>>(200, "application/json"));
  }

  public override async Task HandleAsync(CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new ListLotOccupancyQuery(), cancellationToken);

    var records = result.Value.Select(OccupancyMapping.ToRecord).ToList();
    await Send.OkAsync(records, cancellationToken);
  }
}
