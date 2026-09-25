using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.AccessEventFeatures.Ingest;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.AccessEventFeatures.RecordManual;

public sealed class RecordManualEventRequest
{
  public const string Route = "/lots/{LotId}/manual-events";
  public const string IdempotencyKeyPrefix = "manual:";

  public Guid LotId { get; init; }
  public string Plate { get; init; } = string.Empty;
  public Direction Direction { get; init; }

  [FromHeader("Idempotency-Key")]
  public string IdempotencyKey { get; init; } = string.Empty;
}

public class RecordManualEventEndpoint(IMediator mediator)
  : Endpoint<RecordManualEventRequest, Results<Ok<AccessEventDecisionRecord>, NotFound, ValidationProblem>>
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
        "with a decision body; use an override to let a denied vehicle in. A required Idempotency-Key " +
        "makes a retried submission safe.";
      s.Responses[200] = "Decision recorded";
      s.Responses[400] = "Malformed body or missing Idempotency-Key";
      s.Responses[404] = "Lot with specified ID not found";
    });

    Tags("AccessEvents");

    Description(builder => builder
      .Accepts<RecordManualEventRequest>()
      .Produces<AccessEventDecisionRecord>(200, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<AccessEventDecisionRecord>, NotFound, ValidationProblem>>
    ExecuteAsync(RecordManualEventRequest request, CancellationToken cancellationToken)
  {
    var staffId = Guid.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    var command = new IngestAccessEventCommand(
      ParkingLotId.From(request.LotId),
      request.Plate,
      request.Direction,
      EventSource.Manual,
      DateTimeOffset.UtcNow,
      RecordManualEventRequest.IdempotencyKeyPrefix + request.IdempotencyKey,
      staffId,
      staffId);

    var result = await mediator.Send(command, cancellationToken);

    if (result.Value.Reason == DenyReason.LotNotFound) return TypedResults.NotFound();

    return TypedResults.Ok(AccessEventMapping.ToRecord(result.Value));
  }
}

public sealed class RecordManualEventValidator : Validator<RecordManualEventRequest>
{
  private static readonly int MaxIdempotencyKeyLength = 200 - RecordManualEventRequest.IdempotencyKeyPrefix.Length;

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
      .MaximumLength(MaxIdempotencyKeyLength)
      .WithMessage($"Idempotency-Key must be {MaxIdempotencyKeyLength} characters or fewer");
  }
}
