using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.ReservationFeatures.GetActiveBySpace;

public sealed class GetActiveReservationBySpaceRequest
{
  public const string Route = "/spaces/{SpaceId}/reservation";
  public Guid SpaceId { get; init; }
}

public class GetActiveReservationBySpaceEndpoint(IMediator mediator)
  : Endpoint<GetActiveReservationBySpaceRequest, Results<Ok<ReservationRecord>, NoContent>>
{
  public override void Configure()
  {
    Get(GetActiveReservationBySpaceRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Get a space's active reservation";
      s.Description = "Returns the space's current ACTIVE reservation, or 204 No Content if the space isn't currently reserved.";
      s.Responses[200] = "Active reservation returned";
      s.Responses[204] = "Space has no active reservation";
    });

    Tags("Reservations");

    Description(builder => builder
      .Accepts<GetActiveReservationBySpaceRequest>()
      .Produces<ReservationRecord>(200, "application/json")
      .Produces(204));
  }

  public override async Task<Results<Ok<ReservationRecord>, NoContent>>
    ExecuteAsync(GetActiveReservationBySpaceRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
      new GetActiveReservationBySpaceQuery(ParkingSpaceId.From(request.SpaceId)), cancellationToken);

    return result.Value is null
      ? TypedResults.NoContent()
      : TypedResults.Ok(ReservationMapping.ToRecord(result.Value));
  }
}

public sealed class GetActiveReservationBySpaceValidator : Validator<GetActiveReservationBySpaceRequest>
{
  public GetActiveReservationBySpaceValidator()
  {
    RuleFor(x => x.SpaceId)
      .NotEmpty()
      .WithMessage("Space ID is required");
  }
}
