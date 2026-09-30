using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Reservations.Cancel;

public sealed class CancelReservationRequest
{
  public const string Route = "/reservations/{ReservationId}/cancel";
  public Guid ReservationId { get; init; }
}

public class CancelReservationEndpoint(IMediator mediator, ICurrentUser currentUser)
  : Endpoint<CancelReservationRequest, Results<Ok<ReservationResponse>, ValidationProblem, ProblemHttpResult>>
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
      s.Responses[400] = "Reservation is not active";
      s.Responses[404] = "Reservation with specified ID not found";
    });

    Tags("Reservations");

    Description(builder => builder
      .Accepts<CancelReservationRequest>()
      .Produces<ReservationResponse>(200, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<ReservationResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(CancelReservationRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
      new CancelReservationCommand(ReservationId.From(request.ReservationId), currentUser.Id), cancellationToken);

    return result.ToOkResult(reservation => reservation);
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
