using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Extensions;

namespace Parkin.Api.LotFeatures.Update;

public sealed class UpdateLotRequest
{
  public const string Route = "/lots/{LotId}";

  public Guid LotId { get; init; }
  public string? Name { get; init; }
  public string? Address { get; init; }
  public string? Timezone { get; init; }
  public AccessMode? AccessMode { get; init; }
  public FullBehavior? FullBehavior { get; init; }
}

public class UpdateEndpoint(IMediator mediator)
  : Endpoint<UpdateLotRequest, Results<Ok<LotRecord>, NotFound, ProblemHttpResult>>
{
  public override void Configure()
  {
    Patch(UpdateLotRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Update a parking lot";
      s.Description = "Partially updates a parking lot. Only the fields present in the request body are applied.";
      s.Responses[200] = "Lot updated successfully";
      s.Responses[404] = "Lot with specified ID not found";
      s.Responses[400] = "Invalid request data, or a lot with this name already exists";
    });

    Tags("Lots");

    Description(builder => builder
      .Accepts<UpdateLotRequest>()
      .Produces<LotRecord>(200, "application/json")
      .ProducesProblem(404)
      .ProducesProblem(400));
  }

  public override async Task<Results<Ok<LotRecord>, NotFound, ProblemHttpResult>>
    ExecuteAsync(UpdateLotRequest request, CancellationToken cancellationToken)
  {
    var actorIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var actorId = actorIdClaim is null ? (Guid?)null : Guid.Parse(actorIdClaim);
    var command = new UpdateLotCommand(
      ParkingLotId.From(request.LotId),
      request.Name,
      request.Address,
      request.Timezone,
      request.AccessMode,
      request.FullBehavior,
      actorId);

    var result = await mediator.Send(command, cancellationToken);

    return result.ToUpdateResult(LotEnumMapping.ToRecord);
  }
}

public sealed class UpdateLotValidator : Validator<UpdateLotRequest>
{
  public UpdateLotValidator()
  {
    RuleFor(x => x.LotId)
      .NotEmpty()
      .WithMessage("Lot ID is required");

    RuleFor(x => x.Name)
      .MaximumLength(200)
      .WithMessage("Name must not exceed 200 characters")
      .When(x => x.Name is not null);

    RuleFor(x => x.Name)
      .NotEmpty()
      .WithMessage("Name cannot be blank")
      .When(x => x.Name is not null);

    RuleFor(x => x.Timezone)
      .Must(tz => TimeZoneInfo.TryFindSystemTimeZoneById(tz!, out _))
      .WithMessage("Timezone must be a valid IANA time zone identifier")
      .When(x => !string.IsNullOrWhiteSpace(x.Timezone));

    RuleFor(x => x.Timezone)
      .NotEmpty()
      .WithMessage("Timezone cannot be blank")
      .When(x => x.Timezone is not null);
  }
}
