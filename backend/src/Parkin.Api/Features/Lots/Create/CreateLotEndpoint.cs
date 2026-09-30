using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Lots.Create;

public sealed class CreateLotRequest
{
  public string Name { get; init; } = string.Empty;
  public string? Address { get; init; }
  public string Timezone { get; init; } = string.Empty;
  public AccessMode AccessMode { get; init; } = AccessMode.Open;
  public FullBehavior FullBehavior { get; init; } = FullBehavior.Block;
  public LotLayoutRequest? Layout { get; init; }
}

public class CreateLotEndpoint(IMediator mediator, ICurrentUser currentUser)
  : Endpoint<CreateLotRequest, Results<Created<LotResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post("/lots");
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Create a new parking lot";
      s.Description = "Creates a new parking lot with the given name, timezone, address, access mode, and full-lot behavior.";
      s.ExampleRequest = new CreateLotRequest
      {
        Name = "Downtown Garage",
        Address = "100 Main St",
        Timezone = "America/New_York",
        AccessMode = AccessMode.Open,
        FullBehavior = FullBehavior.Block
      };
      s.ResponseExamples[201] = new LotResponse(Guid.Empty, "Downtown Garage", "100 Main St", "America/New_York",
        AccessMode.Open, FullBehavior.Block, LotStatus.Active, Capacity: 0, Layout: null);

      s.Responses[201] = "Lot created successfully";
      s.Responses[400] = "Invalid request data, or a lot with this name already exists";
      s.Responses[409] = "Another lot with this name was created concurrently";
    });

    Tags("Lots");

    Description(builder => builder
      .Accepts<CreateLotRequest>()
      .Produces<LotResponse>(201, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(409));
  }

  public override async Task<Results<Created<LotResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(CreateLotRequest request, CancellationToken cancellationToken)
  {
    var command = new CreateLotCommand(
      request.Name,
      request.Address,
      request.Timezone,
      request.AccessMode,
      request.FullBehavior,
      currentUser.Id,
      request.Layout?.ToValue().Value);

    var result = await mediator.Send(command, cancellationToken);

    return result.ToCreatedResult(lot => $"/lots/{lot.Id}", lot => lot);
  }
}

public sealed class CreateLotValidator : Validator<CreateLotRequest>
{
  public CreateLotValidator()
  {
    RuleFor(x => x.Name)
      .NotEmpty()
      .WithMessage("Name is required")
      .MaximumLength(ParkingLot.NameMaxLength)
      .WithMessage($"Name must not exceed {ParkingLot.NameMaxLength} characters");

    RuleFor(x => x.Timezone)
      .NotEmpty()
      .WithMessage("Timezone is required")
      .Must(tz => TimeZoneInfo.TryFindSystemTimeZoneById(tz, out _))
      .WithMessage("Timezone must be a valid IANA time zone identifier")
      .When(x => !string.IsNullOrWhiteSpace(x.Timezone));

    RuleFor(x => x.Layout!)
      .SetValidator(new LotLayoutRequestValidator())
      .When(x => x.Layout is not null);
  }
}
