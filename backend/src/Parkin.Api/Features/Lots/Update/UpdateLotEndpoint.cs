using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Lots.Update;

public sealed class UpdateLotRequest
{
  public const string Route = "/lots/{LotId}";

  public Guid LotId { get; init; }
  public string? Name { get; init; }
  public string? Address { get; init; }
  public string? Timezone { get; init; }
  public AccessMode? AccessMode { get; init; }
  public FullBehavior? FullBehavior { get; init; }
  public LotLayoutRequest? Layout { get; init; }
  public bool ClearLayout { get; init; }
}

public class UpdateLotEndpoint(IMediator mediator, ICurrentUser currentUser)
  : Endpoint<UpdateLotRequest, Results<Ok<LotResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Patch(UpdateLotRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Update a parking lot";
      s.Description = "Partially updates a parking lot. Only the fields present in the request body are applied. " +
        "layout sets the ground footprint and level count; clearLayout=true removes it. " +
        "Detail, access-mode and full-behavior changes are audited separately; unchanged fields are not audited.";
      s.Responses[200] = "Lot updated successfully";
      s.Responses[404] = "Lot with specified ID not found";
      s.Responses[400] = "Invalid request data, or a lot with this name already exists";
      s.Responses[409] = "Another lot took this name concurrently";
    });

    Tags("Lots");

    Description(builder => builder
      .Accepts<UpdateLotRequest>()
      .Produces<LotResponse>(200, "application/json")
      .ProducesProblem(404)
      .ProducesProblem(400)
      .ProducesProblem(409));
  }

  public override async Task<Results<Ok<LotResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(UpdateLotRequest request, CancellationToken cancellationToken)
  {
    var command = new UpdateLotCommand(
      ParkingLotId.From(request.LotId),
      request.Name,
      request.Address,
      request.Timezone,
      request.AccessMode,
      request.FullBehavior,
      currentUser.Id,
      request.Layout?.ToValue().Value,
      request.ClearLayout);

    var result = await mediator.Send(command, cancellationToken);

    return result.ToOkResult(lot => lot);
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
      .NotEmpty()
      .WithMessage("Name cannot be blank")
      .MaximumLength(ParkingLot.NameMaxLength)
      .WithMessage($"Name must not exceed {ParkingLot.NameMaxLength} characters")
      .When(x => x.Name is not null);

    RuleFor(x => x.Timezone)
      .NotEmpty()
      .WithMessage("Timezone cannot be blank")
      .When(x => x.Timezone is not null);

    RuleFor(x => x.Timezone)
      .Must(tz => TimeZoneInfo.TryFindSystemTimeZoneById(tz!, out _))
      .WithMessage("Timezone must be a valid IANA time zone identifier")
      .When(x => !string.IsNullOrWhiteSpace(x.Timezone));

    RuleFor(x => x.Layout!)
      .SetValidator(new LotLayoutRequestValidator())
      .When(x => x.Layout is not null);

    RuleFor(x => x.ClearLayout)
      .Equal(false)
      .WithMessage("Send either layout or clearLayout, not both")
      .When(x => x.Layout is not null);
  }
}
