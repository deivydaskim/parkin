using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.AccessEventFeatures.Ingest;

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

// Keeps the /api/v1 prefix the architecture doc specifies: this is the only route an external
// system integrates against, unlike the staff-facing endpoints.
public class IngestAccessEventEndpoint(IMediator mediator)
  : Endpoint<IngestAccessEventRequest, Results<Ok<AccessEventDecisionRecord>, ValidationProblem>>
{
  public override void Configure()
  {
    Post(IngestAccessEventRequest.Route);

    AuthSchemes(AuthenticationSchemes.ApiKey);

    Summary(s =>
    {
      s.Summary = "Record a gate access event and return the entry decision";
      s.Description = "Called by the LPR / barrier system on every ENTER and EXIT. Authenticated with " +
        "the X-Api-Key header; a required Idempotency-Key makes retries safe — a replayed key returns " +
        "the original decision verbatim and never double-counts. An ALLOWed ENTER opens a parking " +
        "session; an EXIT closes the most-recent open session for that plate in that lot, or is " +
        "recorded as a NoOpenSession anomaly that leaves occupancy untouched. Every genuine outcome " +
        "is a 200 with a decision body, including denials and an unknown lot id.";
      s.Responses[200] = "Decision recorded";
      s.Responses[400] = "Malformed body or missing Idempotency-Key";
      s.Responses[401] = "Missing, invalid, or revoked API key";
    });

    Tags("AccessEvents");

    Description(builder => builder
      .Accepts<IngestAccessEventRequest>()
      .Produces<AccessEventDecisionRecord>(200, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(401));
  }

  public override async Task<Results<Ok<AccessEventDecisionRecord>, ValidationProblem>>
    ExecuteAsync(IngestAccessEventRequest request, CancellationToken cancellationToken)
  {
    // Set by ApiKeyAuthenticationHandler - the acting credential is the API key, not a person.
    var actorIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var actorId = actorIdClaim is null ? (Guid?)null : Guid.Parse(actorIdClaim);

    var command = new IngestAccessEventCommand(
      ParkingLotId.From(request.LotId),
      request.Plate,
      request.Direction,
      request.Source,
      request.OccurredAt,
      request.IdempotencyKey,
      actorId);

    var result = await mediator.Send(command, cancellationToken);

    return TypedResults.Ok(AccessEventMapping.ToRecord(result.Value));
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
      .MaximumLength(200)
      .WithMessage("Idempotency-Key must be 200 characters or fewer");

    RuleFor(x => x.OccurredAt)
      .NotEqual(default(DateTimeOffset))
      .WithMessage("Occurred-at timestamp is required");
  }
}
