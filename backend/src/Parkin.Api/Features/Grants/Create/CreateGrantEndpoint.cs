using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Grants.Create;

public sealed class CreateGrantRequest
{
  public const string Route = "/grants";

  public Guid DriverId { get; init; }
  public Guid LotId { get; init; }
  public DateTimeOffset? ValidFrom { get; init; }
  public DateTimeOffset? ValidTo { get; init; }
}

public class CreateGrantEndpoint(IMediator mediator, ICurrentUser currentUser)
  : Endpoint<CreateGrantRequest, Results<Created<GrantResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(CreateGrantRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Grant a driver access to a lot";
      s.Description = "Ties a driver to a lot for an optional date window. Only meaningful for RESTRICTED lots; has no effect on OPEN lots.";
      s.Responses[201] = "Grant created successfully";
      s.Responses[400] = "Invalid request data, lot is not active, or an active grant already exists for this driver and lot";
      s.Responses[404] = "Driver or lot with specified ID not found";
      s.Responses[409] = "An active grant for this driver and lot was created concurrently by another request";
    });

    Tags("Grants");

    Description(builder => builder
      .Accepts<CreateGrantRequest>()
      .Produces<GrantResponse>(201, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(404)
      .ProducesProblem(409));
  }

  public override async Task<Results<Created<GrantResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(CreateGrantRequest request, CancellationToken cancellationToken)
  {
    var command = new CreateGrantCommand(
      DriverId.From(request.DriverId),
      ParkingLotId.From(request.LotId),
      request.ValidFrom,
      request.ValidTo,
      currentUser.Id);

    var result = await mediator.Send(command, cancellationToken);

    return result.ToCreatedResult(grant => $"/grants/{grant.Id}", grant => grant);
  }
}

public sealed class CreateGrantValidator : Validator<CreateGrantRequest>
{
  public CreateGrantValidator()
  {
    RuleFor(x => x.DriverId)
      .NotEmpty()
      .WithMessage("Driver ID is required");

    RuleFor(x => x.LotId)
      .NotEmpty()
      .WithMessage("Lot ID is required");

    RuleFor(x => x.ValidTo)
      .GreaterThanOrEqualTo(x => x.ValidFrom)
      .When(x => x.ValidFrom.HasValue && x.ValidTo.HasValue)
      .WithMessage("Valid-to must not be before valid-from");
  }
}
