using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Drivers.Archive;

public sealed class ArchiveDriverRequest
{
  public const string Route = "/drivers/{DriverId}/archive";
  public Guid DriverId { get; init; }
}

public class ArchiveDriverEndpoint(IMediator mediator, ICurrentUser currentUser)
  : Endpoint<ArchiveDriverRequest, Results<Ok<DriverResponse>, ValidationProblem, ProblemHttpResult>>
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
      .Produces<DriverResponse>(200, "application/json")
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<DriverResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(ArchiveDriverRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
      new ArchiveDriverCommand(DriverId.From(request.DriverId), currentUser.Id), cancellationToken);

    return result.ToOkResult(driver => driver);
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
