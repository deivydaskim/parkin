using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Extensions;

namespace Parkin.Api.DriverFeatures.GetById;

public sealed class GetDriverByIdRequest
{
  public const string Route = "/drivers/{DriverId}";
  public Guid DriverId { get; init; }
}

public class GetDriverEndpoint(IMediator mediator)
  : Endpoint<GetDriverByIdRequest, Results<Ok<DriverRecord>, NotFound, ProblemHttpResult>>
{
  public override void Configure()
  {
    Get(GetDriverByIdRequest.Route);
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
      .Accepts<GetDriverByIdRequest>()
      .Produces<DriverRecord>(200, "application/json")
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<DriverRecord>, NotFound, ProblemHttpResult>>
    ExecuteAsync(GetDriverByIdRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new GetDriverQuery(DriverId.From(request.DriverId)), cancellationToken);

    return result.ToGetByIdResult(DriverMapping.ToRecord);
  }
}

public sealed class GetDriverByIdValidator : Validator<GetDriverByIdRequest>
{
  public GetDriverByIdValidator()
  {
    RuleFor(x => x.DriverId)
      .NotEmpty()
      .WithMessage("Driver ID is required");
  }
}
