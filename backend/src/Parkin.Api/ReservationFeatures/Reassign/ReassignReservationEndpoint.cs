using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Extensions;

namespace Parkin.Api.ReservationFeatures.Reassign;

public sealed class ReassignReservationRequest
{
  public const string Route = "/reservations/{ReservationId}/reassign";

  public Guid ReservationId { get; init; }
  public Guid DriverId { get; init; }
}

public class ReassignReservationEndpoint(IMediator mediator)
  : Endpoint<ReassignReservationRequest, Results<Ok<ReservationRecord>, ValidationProblem, NotFound, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(ReassignReservationRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Reassign a reservation to another driver";
      s.Description = "Atomically ends the reservation and creates a new ACTIVE reservation for the same space " +
        "with the new driver — a single request never leaves two ACTIVE rows for the space, nor a window where " +
        "the space is unreserved. Alternative to a cancel-then-create round trip. Audit-logged.";
      s.Responses[200] = "Reservation reassigned successfully";
      s.Responses[400] = "Reservation is not active, or already belongs to this driver";
      s.Responses[404] = "Reservation or new driver not found";
      s.Responses[409] = "New driver already has an active reservation in this lot";
    });

    Tags("Reservations");

    Description(builder => builder
      .Accepts<ReassignReservationRequest>()
      .Produces<ReservationRecord>(200, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(404)
      .ProducesProblem(409));
  }

  public override async Task<Results<Ok<ReservationRecord>, ValidationProblem, NotFound, ProblemHttpResult>>
    ExecuteAsync(ReassignReservationRequest request, CancellationToken cancellationToken)
  {
    var actorIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var actorId = actorIdClaim is null ? (Guid?)null : Guid.Parse(actorIdClaim);
    var command = new ReassignReservationCommand(
      ReservationId.From(request.ReservationId),
      DriverId.From(request.DriverId),
      actorId);

    var result = await mediator.Send(command, cancellationToken);

    return result.ToOkOrConflictResult(ReservationMapping.ToRecord);
  }
}

public sealed class ReassignReservationValidator : Validator<ReassignReservationRequest>
{
  public ReassignReservationValidator()
  {
    RuleFor(x => x.ReservationId)
      .NotEmpty()
      .WithMessage("Reservation ID is required");

    RuleFor(x => x.DriverId)
      .NotEmpty()
      .WithMessage("Driver ID is required");
  }
}
