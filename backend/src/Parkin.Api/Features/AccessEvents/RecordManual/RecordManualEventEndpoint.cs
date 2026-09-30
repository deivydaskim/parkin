using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Features.AccessEvents.Ingest;
using Parkin.Api.Web;

namespace Parkin.Api.Features.AccessEvents.RecordManual;

public sealed class RecordManualEventRequest
{
  public const string Route = "/lots/{LotId}/manual-events";

  public Guid LotId { get; init; }
  public string Plate { get; init; } = string.Empty;
  public Direction Direction { get; init; }

  [FromHeader("Idempotency-Key")]
  public string IdempotencyKey { get; init; } = string.Empty;
}

public class RecordManualEventEndpoint(IMediator mediator, ICurrentUser currentUser, TimeProvider timeProvider)
  : Endpoint<RecordManualEventRequest, Results<Ok<AccessEventDecisionResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(RecordManualEventRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Manually record an entry or exit for a lot";
      s.Description = "Lets staff log an ENTER or EXIT when the plate reader fails or for a walk-up, for " +
        "a known or an unknown plate. The event runs through the same decision and session logic as the " +
        "gate, is tagged Source=Manual with the acting staff user, and is audit-logged. Denials are a 200 " +
        "with a decision body; use an override to let a denied vehicle in. A required Idempotency-Key, " +
        "scoped to the acting staff user, makes a retried submission safe; reusing it for a different " +
        "lot, plate or direction is rejected with 409. An unknown lot is a 404 and records nothing.";
      s.Responses[200] = "Decision recorded";
      s.Responses[400] = "Malformed body or missing Idempotency-Key";
      s.Responses[404] = "Lot with specified ID not found";
      s.Responses[409] = "Idempotency-Key already used for a different access event";
    });

    Tags("AccessEvents");

    Description(builder => builder
      .Accepts<RecordManualEventRequest>()
      .Produces<AccessEventDecisionResponse>(200, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(404)
      .ProducesProblem(409));
  }

  public override async Task<Results<Ok<AccessEventDecisionResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(RecordManualEventRequest request, CancellationToken cancellationToken)
  {
    var command = new IngestAccessEventCommand(
      ParkingLotId.From(request.LotId),
      request.Plate,
      request.Direction,
      EventSource.Manual,
      timeProvider.GetUtcNow(),
      request.IdempotencyKey,
      AccessEventActor.Staff(currentUser.RequiredId));

    var result = await mediator.Send(command, cancellationToken);

    return result.ToOkResult(decision => decision);
  }
}

public sealed class RecordManualEventValidator : Validator<RecordManualEventRequest>
{
  public RecordManualEventValidator()
  {
    RuleFor(x => x.LotId)
      .NotEmpty()
      .WithMessage("Lot ID is required");

    RuleFor(x => x.Plate)
      .NotEmpty()
      .WithMessage("Plate is required")
      .MaximumLength(20)
      .WithMessage("Plate must be 20 characters or fewer");

    RuleFor(x => x.Direction)
      .IsInEnum()
      .WithMessage("Direction must be Enter or Exit");

    RuleFor(x => x.IdempotencyKey)
      .NotEmpty()
      .WithMessage("Idempotency-Key header is required")
      .MaximumLength(AccessEvent.IdempotencyKeyMaxLength)
      .WithMessage($"Idempotency-Key must be {AccessEvent.IdempotencyKeyMaxLength} characters or fewer");
  }
}
