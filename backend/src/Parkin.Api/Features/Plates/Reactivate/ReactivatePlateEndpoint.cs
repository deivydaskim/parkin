using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Plates.Reactivate;

public sealed class ReactivatePlateRequest
{
  public const string Route = "/plates/{PlateId}/reactivate";
  public Guid PlateId { get; init; }
}

public class ReactivatePlateEndpoint(IMediator mediator, ICurrentUser currentUser)
  : Endpoint<ReactivatePlateRequest, Results<Ok<PlateResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(ReactivatePlateRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Reactivate a plate";
      s.Description = "Reactivates a deactivated plate. Plate numbers stay unique while inactive, so no other plate can hold the same number.";
      s.Responses[200] = "Plate reactivated successfully";
      s.Responses[404] = "Plate with specified ID not found";
    });

    Tags("Drivers");

    Description(builder => builder
      .Accepts<ReactivatePlateRequest>()
      .Produces<PlateResponse>(200, "application/json")
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<PlateResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(ReactivatePlateRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
      new ReactivatePlateCommand(PlateId.From(request.PlateId), currentUser.Id), cancellationToken);

    return result.ToOkResult(plate => plate);
  }
}

public sealed class ReactivatePlateValidator : Validator<ReactivatePlateRequest>
{
  public ReactivatePlateValidator()
  {
    RuleFor(x => x.PlateId)
      .NotEmpty()
      .WithMessage("Plate ID is required");
  }
}
