using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Extensions;

namespace Parkin.Api.ReservationFeatures.Create;

public sealed class CreateReservationRequest
{
  public const string Route = "/reservations";

  public Guid SpaceId { get; init; }
  public Guid DriverId { get; init; }
}

public class CreateReservationEndpoint(IMediator mediator)
  : Endpoint<CreateReservationRequest, Results<Created<ReservationRecord>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(CreateReservationRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Reserve a space for a driver";
      s.Description = "Ties a space to a driver. A space may have at most one ACTIVE reservation, and a driver at most one ACTIVE reservation per lot. Creating a reservation sets the space type to RESERVED.";
      s.Responses[201] = "Reservation created successfully";
      s.Responses[400] = "Invalid request data or space is not active";
      s.Responses[404] = "Space or driver with specified ID not found";
      s.Responses[409] = "Space or driver already has an active reservation";
    });

    Tags("Reservations");

    Description(builder => builder
      .Accepts<CreateReservationRequest>()
      .Produces<ReservationRecord>(201, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(404)
      .ProducesProblem(409));
  }

  public override async Task<Results<Created<ReservationRecord>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(CreateReservationRequest request, CancellationToken cancellationToken)
  {
    var actorIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var actorId = actorIdClaim is null ? (Guid?)null : Guid.Parse(actorIdClaim);
    var command = new CreateReservationCommand(
      Domain.ParkingLotAggregate.ParkingSpaceId.From(request.SpaceId),
      Domain.DriverAggregate.DriverId.From(request.DriverId),
      actorId);

    var result = await mediator.Send(command, cancellationToken);

    return result.ToCreatedOrConflictResult(
      reservation => $"/reservations/{reservation.Id.Value}",
      ReservationMapping.ToRecord);
  }
}

public sealed class CreateReservationValidator : Validator<CreateReservationRequest>
{
  public CreateReservationValidator()
  {
    RuleFor(x => x.SpaceId)
      .NotEmpty()
      .WithMessage("Space ID is required");

    RuleFor(x => x.DriverId)
      .NotEmpty()
      .WithMessage("Driver ID is required");
  }
}
