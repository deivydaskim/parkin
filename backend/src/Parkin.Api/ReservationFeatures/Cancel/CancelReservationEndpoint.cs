using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Extensions;

namespace Parkin.Api.ReservationFeatures.Cancel;

public sealed class CancelReservationRequest
{
  public const string Route = "/reservations/{ReservationId}/cancel";
  public Guid ReservationId { get; init; }
}

public class CancelReservationEndpoint(IMediator mediator)
  : Endpoint<CancelReservationRequest, Results<Ok<ReservationRecord>, NotFound, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(CancelReservationRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Cancel a reservation";
      s.Description = "Ends an active reservation, freeing the driver from the space. The space's type is left as-is.";
      s.Responses[200] = "Reservation cancelled successfully";
      s.Responses[404] = "Reservation with specified ID not found";
    });

    Tags("Reservations");

    Description(builder => builder
      .Accepts<CancelReservationRequest>()
      .Produces<ReservationRecord>(200, "application/json")
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<ReservationRecord>, NotFound, ProblemHttpResult>>
    ExecuteAsync(CancelReservationRequest request, CancellationToken cancellationToken)
  {
    var actorIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var actorId = actorIdClaim is null ? (Guid?)null : Guid.Parse(actorIdClaim);
    var result = await mediator.Send(
      new CancelReservationCommand(ReservationId.From(request.ReservationId), actorId), cancellationToken);

    return result.ToUpdateResult(ReservationMapping.ToRecord);
  }
}

public sealed class CancelReservationValidator : Validator<CancelReservationRequest>
{
  public CancelReservationValidator()
  {
    RuleFor(x => x.ReservationId)
      .NotEmpty()
      .WithMessage("Reservation ID is required");
  }
}
