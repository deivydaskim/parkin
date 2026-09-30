using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.AccessEvents.Ingest;

public sealed class IngestAccessEventRequest
{
  public const string Route = "/api/v1/access-events";

  public Guid LotId { get; init; }
  public string Plate { get; init; } = string.Empty;
  public Direction Direction { get; init; }
  public DateTimeOffset OccurredAt { get; init; }
  public EventSource Source { get; init; } = EventSource.Lpr;

  [FromHeader("Idempotency-Key")]
  public string IdempotencyKey { get; init; } = string.Empty;
}

public class IngestAccessEventEndpoint(IMediator mediator, ICurrentUser currentUser)
  : Endpoint<IngestAccessEventRequest, Results<Ok<AccessEventDecisionResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(IngestAccessEventRequest.Route);

    AuthSchemes(AuthenticationSchemes.ApiKey);

    Summary(s =>
    {
      s.Summary = "Record a gate access event and return the entry decision";
      s.Description = "Called by the LPR / barrier system on every ENTER and EXIT. Authenticated with " +
        "the X-Api-Key header; a required Idempotency-Key makes retries safe. Keys are scoped to the " +
        "calling API key: replaying a key with the same lot, plate and direction returns the original " +
        "decision verbatim and never double-counts, while reusing it for a different lot, plate or " +
        "direction is rejected with 409. An ALLOWed ENTER opens a parking session; an EXIT closes the " +
        "most-recent open session for that plate in that lot, or is recorded as a NoOpenSession anomaly " +
        "that leaves occupancy untouched. Every genuine outcome is a 200 with a decision body, including " +
        "denials and an unknown lot id.";
      s.Responses[200] = "Decision recorded";
      s.Responses[400] = "Malformed body or missing Idempotency-Key";
      s.Responses[401] = "Missing, invalid, or revoked API key";
      s.Responses[409] = "Idempotency-Key already used for a different access event";
    });

    Tags("AccessEvents");

    Description(builder => builder
      .Accepts<IngestAccessEventRequest>()
      .Produces<AccessEventDecisionResponse>(200, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(401)
      .ProducesProblem(409));
  }

  public override async Task<Results<Ok<AccessEventDecisionResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(IngestAccessEventRequest request, CancellationToken cancellationToken)
  {
    var command = new IngestAccessEventCommand(
      ParkingLotId.From(request.LotId),
      request.Plate,
      request.Direction,
      request.Source,
      request.OccurredAt,
      request.IdempotencyKey,
      AccessEventActor.ApiKey(currentUser.RequiredId));

    var result = await mediator.Send(command, cancellationToken);

    return result.ToOkResult(decision => decision);
  }
}

public sealed class IngestAccessEventValidator : Validator<IngestAccessEventRequest>
{
  public IngestAccessEventValidator()
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

    RuleFor(x => x.Source)
      .IsInEnum()
      .WithMessage("Source must be Lpr or Manual");

    RuleFor(x => x.IdempotencyKey)
      .NotEmpty()
      .WithMessage("Idempotency-Key header is required")
      .MaximumLength(AccessEvent.IdempotencyKeyMaxLength)
      .WithMessage($"Idempotency-Key must be {AccessEvent.IdempotencyKeyMaxLength} characters or fewer");

    RuleFor(x => x.OccurredAt)
      .NotEqual(default(DateTimeOffset))
      .WithMessage("Occurred-at timestamp is required");
  }
}
