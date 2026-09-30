using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Drivers.GetById;

public sealed class GetDriverRequest
{
  public const string Route = "/drivers/{DriverId}";
  public Guid DriverId { get; init; }
}

public class GetDriverEndpoint(IMediator mediator)
  : Endpoint<GetDriverRequest, Results<Ok<DriverResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Get(GetDriverRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Get a driver by ID";
      s.Description = "Retrieves a specific driver record by its unique identifier.";
      s.Responses[200] = "Driver found and returned successfully";
      s.Responses[404] = "Driver with specified ID not found";
    });

    Tags("Drivers");

    Description(builder => builder
      .Accepts<GetDriverRequest>()
      .Produces<DriverResponse>(200, "application/json")
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<DriverResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(GetDriverRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new GetDriverQuery(DriverId.From(request.DriverId)), cancellationToken);

    return result.ToOkResult(driver => driver);
  }
}

public sealed class GetDriverValidator : Validator<GetDriverRequest>
{
  public GetDriverValidator()
  {
    RuleFor(x => x.DriverId)
      .NotEmpty()
      .WithMessage("Driver ID is required");
  }
}
