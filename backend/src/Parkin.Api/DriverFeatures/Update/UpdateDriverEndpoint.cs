using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Extensions;

namespace Parkin.Api.DriverFeatures.Update;

public sealed class UpdateDriverRequest
{
  public const string Route = "/drivers/{DriverId}";

  public Guid DriverId { get; init; }
  public string? Name { get; init; }
  public string? Contact { get; init; }
}

public class UpdateDriverEndpoint(IMediator mediator)
  : Endpoint<UpdateDriverRequest, Results<Ok<DriverRecord>, NotFound, ProblemHttpResult>>
{
  public override void Configure()
  {
    Patch(UpdateDriverRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Update a driver";
      s.Description = "Partially updates a driver's name and/or contact. Only the fields present in the request body are applied.";
      s.Responses[200] = "Driver updated successfully";
      s.Responses[404] = "Driver with specified ID not found";
      s.Responses[400] = "Invalid request data";
    });

    Tags("Drivers");

    Description(builder => builder
      .Accepts<UpdateDriverRequest>()
      .Produces<DriverRecord>(200, "application/json")
      .ProducesProblem(404)
      .ProducesProblem(400));
  }

  public override async Task<Results<Ok<DriverRecord>, NotFound, ProblemHttpResult>>
    ExecuteAsync(UpdateDriverRequest request, CancellationToken cancellationToken)
  {
    var actorIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var actorId = actorIdClaim is null ? (Guid?)null : Guid.Parse(actorIdClaim);
    var command = new UpdateDriverCommand(
      DriverId.From(request.DriverId), request.Name, request.Contact, actorId);

    var result = await mediator.Send(command, cancellationToken);

    return result.ToUpdateResult(DriverMapping.ToRecord);
  }
}

public sealed class UpdateDriverValidator : Validator<UpdateDriverRequest>
{
  public UpdateDriverValidator()
  {
    RuleFor(x => x.DriverId)
      .NotEmpty()
      .WithMessage("Driver ID is required");

    RuleFor(x => x.Name)
      .NotEmpty()
      .WithMessage("Name cannot be blank")
      .MaximumLength(200)
      .WithMessage("Name must not exceed 200 characters")
      .When(x => x.Name is not null);

    RuleFor(x => x.Contact)
      .MaximumLength(300)
      .WithMessage("Contact must not exceed 300 characters")
      .When(x => x.Contact is not null);
  }
}
