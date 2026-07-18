using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.PlateFeatures.Reassign;

public sealed class ReassignPlateRequest
{
  public const string Route = "/plates/{PlateId}/reassign";

  public Guid PlateId { get; init; }
  public Guid TargetDriverId { get; init; }
}

public class ReassignPlateEndpoint(IMediator mediator)
  : Endpoint<ReassignPlateRequest, Results<Ok<PlateRecord>, ValidationProblem, NotFound, ProblemHttpResult>>
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
      .Produces<PlateRecord>(200, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<PlateRecord>, ValidationProblem, NotFound, ProblemHttpResult>>
    ExecuteAsync(ReassignPlateRequest request, CancellationToken cancellationToken)
  {
    var actorIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var actorId = actorIdClaim is null ? (Guid?)null : Guid.Parse(actorIdClaim);
    var command = new ReassignPlateCommand(
      PlateId.From(request.PlateId), DriverId.From(request.TargetDriverId), actorId);

    var result = await mediator.Send(command, cancellationToken);

    return result.Status switch
    {
      ResultStatus.Ok => TypedResults.Ok(PlateMapping.ToRecord(result.Value)),
      ResultStatus.NotFound => TypedResults.NotFound(),
      ResultStatus.Invalid => TypedResults.ValidationProblem(
        result.ValidationErrors
          .GroupBy(e => e.Identifier ?? string.Empty)
          .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
      _ => TypedResults.Problem(
        title: "Reassign failed",
        detail: string.Join("; ", result.Errors),
        statusCode: StatusCodes.Status400BadRequest)
    };
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
