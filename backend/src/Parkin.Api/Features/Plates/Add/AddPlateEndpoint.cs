using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Plates.Add;

public sealed class AddPlateRequest
{
  public const string Route = "/drivers/{DriverId}/plates";

  public Guid DriverId { get; init; }
  public string PlateNumber { get; init; } = string.Empty;
}

public class AddPlateEndpoint(IMediator mediator, ICurrentUser currentUser)
  : Endpoint<AddPlateRequest, Results<Created<PlateResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(AddPlateRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Add a plate to a driver";
      s.Description = "Adds a new plate to a driver. The plate number is normalized and must be unique across the instance.";
      s.Responses[201] = "Plate added successfully";
      s.Responses[400] = "Invalid request data, or a driver with this plate already exists";
      s.Responses[404] = "Driver with specified ID not found";
      s.Responses[409] = "The plate was registered concurrently by another request";
    });

    Tags("Drivers");

    Description(builder => builder
      .Accepts<AddPlateRequest>()
      .Produces<PlateResponse>(201, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(404)
      .ProducesProblem(409));
  }

  public override async Task<Results<Created<PlateResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(AddPlateRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
      new AddPlateCommand(DriverId.From(request.DriverId), request.PlateNumber, currentUser.Id), cancellationToken);

    return result.ToCreatedResult(plate => $"/plates/{plate.Id}", plate => plate);
  }
}

public sealed class AddPlateValidator : Validator<AddPlateRequest>
{
  public AddPlateValidator()
  {
    RuleFor(x => x.DriverId)
      .NotEmpty()
      .WithMessage("Driver ID is required");

    RuleFor(x => x.PlateNumber)
      .NotEmpty()
      .WithMessage("Plate number is required")
      .MaximumLength(20)
      .WithMessage("Plate number must not exceed 20 characters");
  }
}
