using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Plates.Reassign;

public sealed class ReassignPlateRequest
{
  public const string Route = "/plates/{PlateId}/reassign";

  public Guid PlateId { get; init; }
  public Guid TargetDriverId { get; init; }
}

public class ReassignPlateEndpoint(IMediator mediator, ICurrentUser currentUser)
  : Endpoint<ReassignPlateRequest, Results<Ok<PlateResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(ReassignPlateRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Reassign a plate to another driver";
      s.Description = "Moves a plate from its current driver to a different driver. Audit-logged.";
      s.Responses[200] = "Plate reassigned successfully";
      s.Responses[400] = "Target driver already owns this plate";
      s.Responses[404] = "Plate or target driver not found";
    });

    Tags("Drivers");

    Description(builder => builder
      .Accepts<ReassignPlateRequest>()
      .Produces<PlateResponse>(200, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<PlateResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(ReassignPlateRequest request, CancellationToken cancellationToken)
  {
    var command = new ReassignPlateCommand(
      PlateId.From(request.PlateId), DriverId.From(request.TargetDriverId), currentUser.Id);

    var result = await mediator.Send(command, cancellationToken);

    return result.ToOkResult(plate => plate);
  }
}

public sealed class ReassignPlateValidator : Validator<ReassignPlateRequest>
{
  public ReassignPlateValidator()
  {
    RuleFor(x => x.PlateId)
      .NotEmpty()
      .WithMessage("Plate ID is required");

    RuleFor(x => x.TargetDriverId)
      .NotEmpty()
      .WithMessage("Target driver ID is required");
  }
}
