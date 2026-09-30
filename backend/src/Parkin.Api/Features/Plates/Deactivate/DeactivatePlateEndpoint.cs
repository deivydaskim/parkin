using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Plates.Deactivate;

public sealed class DeactivatePlateRequest
{
  public const string Route = "/plates/{PlateId}/deactivate";
  public Guid PlateId { get; init; }
}

public class DeactivatePlateEndpoint(IMediator mediator, ICurrentUser currentUser)
  : Endpoint<DeactivatePlateRequest, Results<Ok<PlateResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(DeactivatePlateRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Deactivate a plate";
      s.Description = "Soft-deactivates a plate. Preserves history; the plate stays uniquely reserved (not reusable) while inactive.";
      s.Responses[200] = "Plate deactivated successfully";
      s.Responses[404] = "Plate with specified ID not found";
    });

    Tags("Drivers");

    Description(builder => builder
      .Accepts<DeactivatePlateRequest>()
      .Produces<PlateResponse>(200, "application/json")
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<PlateResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(DeactivatePlateRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
      new DeactivatePlateCommand(PlateId.From(request.PlateId), currentUser.Id), cancellationToken);

    return result.ToOkResult(plate => plate);
  }
}

public sealed class DeactivatePlateValidator : Validator<DeactivatePlateRequest>
{
  public DeactivatePlateValidator()
  {
    RuleFor(x => x.PlateId)
      .NotEmpty()
      .WithMessage("Plate ID is required");
  }
}
