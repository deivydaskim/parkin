using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Extensions;

namespace Parkin.Api.DriverFeatures.Create;

public sealed class CreateDriverRequest
{
  public string Name { get; init; } = string.Empty;
  public string? Contact { get; init; }
}

public class CreateDriverEndpoint(IMediator mediator)
  : Endpoint<CreateDriverRequest, Results<Created<DriverRecord>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post("/drivers");
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Create a new driver";
      s.Description = "Creates a new driver record with a name and optional contact.";
      s.Responses[201] = "Driver created successfully";
      s.Responses[400] = "Invalid request data";
    });

    Tags("Drivers");

    Description(builder => builder
      .Accepts<CreateDriverRequest>()
      .Produces<DriverRecord>(201, "application/json")
      .ProducesProblem(400));
  }

  public override async Task<Results<Created<DriverRecord>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(CreateDriverRequest request, CancellationToken cancellationToken)
  {
    var actorIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var actorId = actorIdClaim is null ? (Guid?)null : Guid.Parse(actorIdClaim);
    var command = new CreateDriverCommand(request.Name, request.Contact, actorId);

    var result = await mediator.Send(command, cancellationToken);

    return result.ToCreatedResult(
      driver => $"/drivers/{driver.Id.Value}",
      DriverMapping.ToRecord);
  }
}

public sealed class CreateDriverValidator : Validator<CreateDriverRequest>
{
  public CreateDriverValidator()
  {
    RuleFor(x => x.Name)
      .NotEmpty()
      .WithMessage("Name is required")
      .MaximumLength(200)
      .WithMessage("Name must not exceed 200 characters");

    RuleFor(x => x.Contact)
      .MaximumLength(300)
      .WithMessage("Contact must not exceed 300 characters");
  }
}
