using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Extensions;

namespace Parkin.Api.DriverFeatures.Restore;

public sealed class RestoreDriverRequest
{
  public const string Route = "/drivers/{DriverId}/restore";
  public Guid DriverId { get; init; }
}

public class RestoreDriverEndpoint(IMediator mediator)
  : Endpoint<RestoreDriverRequest, Results<Ok<DriverRecord>, NotFound, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(RestoreDriverRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Restore an archived driver";
      s.Description = "Restores an archived driver back to active.";
      s.Responses[200] = "Driver restored successfully";
      s.Responses[404] = "Driver with specified ID not found";
    });

    Tags("Drivers");

    Description(builder => builder
      .Accepts<RestoreDriverRequest>()
      .Produces<DriverRecord>(200, "application/json")
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<DriverRecord>, NotFound, ProblemHttpResult>>
    ExecuteAsync(RestoreDriverRequest request, CancellationToken cancellationToken)
  {
    var actorIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var actorId = actorIdClaim is null ? (Guid?)null : Guid.Parse(actorIdClaim);
    var result = await mediator.Send(new RestoreDriverCommand(DriverId.From(request.DriverId), actorId), cancellationToken);

    return result.ToUpdateResult(DriverMapping.ToRecord);
  }
}

public sealed class RestoreDriverValidator : Validator<RestoreDriverRequest>
{
  public RestoreDriverValidator()
  {
    RuleFor(x => x.DriverId)
      .NotEmpty()
      .WithMessage("Driver ID is required");
  }
}
