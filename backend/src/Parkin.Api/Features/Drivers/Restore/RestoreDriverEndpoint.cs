using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Drivers.Restore;

public sealed class RestoreDriverRequest
{
  public const string Route = "/drivers/{DriverId}/restore";
  public Guid DriverId { get; init; }
}

public class RestoreDriverEndpoint(IMediator mediator, ICurrentUser currentUser)
  : Endpoint<RestoreDriverRequest, Results<Ok<DriverResponse>, ValidationProblem, ProblemHttpResult>>
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
      .Produces<DriverResponse>(200, "application/json")
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<DriverResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(RestoreDriverRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
      new RestoreDriverCommand(DriverId.From(request.DriverId), currentUser.Id), cancellationToken);

    return result.ToOkResult(driver => driver);
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
