using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Extensions;

namespace Parkin.Api.DriverFeatures.Archive;

public sealed class ArchiveDriverRequest
{
  public const string Route = "/drivers/{DriverId}/archive";
  public Guid DriverId { get; init; }
}

public class ArchiveDriverEndpoint(IMediator mediator)
  : Endpoint<ArchiveDriverRequest, Results<Ok<DriverRecord>, NotFound, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(ArchiveDriverRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Archive a driver";
      s.Description = "Archives a driver. Archived drivers are hidden from the default operational list but remain directly readable by ID.";
      s.Responses[200] = "Driver archived successfully";
      s.Responses[404] = "Driver with specified ID not found";
    });

    Tags("Drivers");

    Description(builder => builder
      .Accepts<ArchiveDriverRequest>()
      .Produces<DriverRecord>(200, "application/json")
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<DriverRecord>, NotFound, ProblemHttpResult>>
    ExecuteAsync(ArchiveDriverRequest request, CancellationToken cancellationToken)
  {
    var actorIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var actorId = actorIdClaim is null ? (Guid?)null : Guid.Parse(actorIdClaim);
    var result = await mediator.Send(new ArchiveDriverCommand(DriverId.From(request.DriverId), actorId), cancellationToken);

    return result.ToUpdateResult(DriverMapping.ToRecord);
  }
}

public sealed class ArchiveDriverValidator : Validator<ArchiveDriverRequest>
{
  public ArchiveDriverValidator()
  {
    RuleFor(x => x.DriverId)
      .NotEmpty()
      .WithMessage("Driver ID is required");
  }
}
