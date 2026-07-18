using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Extensions;

namespace Parkin.Api.PlateFeatures.Reactivate;

public sealed class ReactivatePlateRequest
{
  public const string Route = "/plates/{PlateId}/reactivate";
  public Guid PlateId { get; init; }
}

public class ReactivatePlateEndpoint(IMediator mediator)
  : Endpoint<ReactivatePlateRequest, Results<Ok<PlateRecord>, NotFound, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(ReactivatePlateRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Reactivate a plate";
      s.Description = "Reactivates a deactivated plate. Fails if another plate already holds the same normalized number — deactivate that one first.";
      s.Responses[200] = "Plate reactivated successfully";
      s.Responses[404] = "Plate with specified ID not found";
      s.Responses[400] = "A plate with this number already exists";
    });

    Tags("Drivers");

    Description(builder => builder
      .Accepts<ReactivatePlateRequest>()
      .Produces<PlateRecord>(200, "application/json")
      .ProducesProblem(404)
      .ProducesProblem(400));
  }

  public override async Task<Results<Ok<PlateRecord>, NotFound, ProblemHttpResult>>
    ExecuteAsync(ReactivatePlateRequest request, CancellationToken cancellationToken)
  {
    var actorIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var actorId = actorIdClaim is null ? (Guid?)null : Guid.Parse(actorIdClaim);
    var result = await mediator.Send(new ReactivatePlateCommand(PlateId.From(request.PlateId), actorId), cancellationToken);

    return result.ToUpdateResult(PlateMapping.ToRecord);
  }
}

public sealed class ReactivatePlateValidator : Validator<ReactivatePlateRequest>
{
  public ReactivatePlateValidator()
  {
    RuleFor(x => x.PlateId)
      .NotEmpty()
      .WithMessage("Plate ID is required");
  }
}
