using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Extensions;

namespace Parkin.Api.PlateFeatures.Deactivate;

public sealed class DeactivatePlateRequest
{
  public const string Route = "/plates/{PlateId}/deactivate";
  public Guid PlateId { get; init; }
}

public class DeactivatePlateEndpoint(IMediator mediator)
  : Endpoint<DeactivatePlateRequest, Results<Ok<PlateRecord>, NotFound, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(DeactivatePlateRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Deactivate a plate";
      s.Description = "Soft-deactivates a plate. Preserves history; the plate stays uniquely reserved (not reusable) while inactive.";
      s.Responses[200] = "Plate deactivated successfully";
      s.Responses[404] = "Plate with specified ID not found";
    });

    Tags("Drivers");

    Description(builder => builder
      .Accepts<DeactivatePlateRequest>()
      .Produces<PlateRecord>(200, "application/json")
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<PlateRecord>, NotFound, ProblemHttpResult>>
    ExecuteAsync(DeactivatePlateRequest request, CancellationToken cancellationToken)
  {
    var actorIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var actorId = actorIdClaim is null ? (Guid?)null : Guid.Parse(actorIdClaim);
    var result = await mediator.Send(new DeactivatePlateCommand(PlateId.From(request.PlateId), actorId), cancellationToken);

    return result.ToUpdateResult(PlateMapping.ToRecord);
  }
}

public sealed class DeactivatePlateValidator : Validator<DeactivatePlateRequest>
{
  public DeactivatePlateValidator()
  {
    RuleFor(x => x.PlateId)
      .NotEmpty()
      .WithMessage("Plate ID is required");
  }
}
