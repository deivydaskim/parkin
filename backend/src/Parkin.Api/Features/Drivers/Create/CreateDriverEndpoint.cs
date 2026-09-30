using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Drivers.Create;

public sealed class CreateDriverRequest
{
  public const string Route = "/drivers";

  public string Name { get; init; } = string.Empty;
  public string? Contact { get; init; }
}

public class CreateDriverEndpoint(IMediator mediator, ICurrentUser currentUser)
  : Endpoint<CreateDriverRequest, Results<Created<DriverResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(CreateDriverRequest.Route);
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
      .Produces<DriverResponse>(201, "application/json")
      .ProducesProblem(400));
  }

  public override async Task<Results<Created<DriverResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(CreateDriverRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
      new CreateDriverCommand(request.Name, request.Contact, currentUser.Id), cancellationToken);

    return result.ToCreatedResult(driver => $"/drivers/{driver.Id}", driver => driver);
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
