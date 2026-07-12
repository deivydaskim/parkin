using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Extensions;

namespace Parkin.Api.LotFeatures.Create;

public sealed class CreateLotRequest
{
  public string Name { get; init; } = string.Empty;
  public string? Address { get; init; }
  public string Timezone { get; init; } = string.Empty;
  public string AccessMode { get; init; } = "OPEN";
  public string FullBehavior { get; init; } = "BLOCK";
}

public class CreateEndpoint(IMediator mediator)
  : Endpoint<CreateLotRequest, Results<Created<LotRecord>, ValidationProblem, ProblemHttpResult>>
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
        AccessMode = "OPEN",
        FullBehavior = "BLOCK"
      };
      s.ResponseExamples[201] = new LotRecord(Guid.Empty, "Downtown Garage", "100 Main St", "America/New_York", "OPEN", "BLOCK", "ACTIVE", 0);

      s.Responses[201] = "Lot created successfully";
      s.Responses[400] = "Invalid request data, or a lot with this name already exists";
    });

    Tags("Lots");

    Description(builder => builder
      .Accepts<CreateLotRequest>()
      .Produces<LotRecord>(201, "application/json")
      .ProducesProblem(400));
  }

  public override async Task<Results<Created<LotRecord>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(CreateLotRequest request, CancellationToken cancellationToken)
  {
    var command = new CreateLotCommand(
      request.Name,
      request.Address,
      request.Timezone,
      LotEnumMapping.ParseAccessMode(request.AccessMode),
      LotEnumMapping.ParseFullBehavior(request.FullBehavior));

    var result = await mediator.Send(command, cancellationToken);

    return result.ToCreatedResult(
      lot => $"/lots/{lot.Id.Value}",
      LotEnumMapping.ToRecord);
  }
}

public sealed class CreateLotValidator : Validator<CreateLotRequest>
{
  public CreateLotValidator()
  {
    RuleFor(x => x.Name)
      .NotEmpty()
      .WithMessage("Name is required")
      .MaximumLength(200)
      .WithMessage("Name must not exceed 200 characters");

    RuleFor(x => x.Timezone)
      .NotEmpty()
      .WithMessage("Timezone is required")
      .Must(tz => TimeZoneInfo.TryFindSystemTimeZoneById(tz, out _))
      .WithMessage("Timezone must be a valid IANA time zone identifier")
      .When(x => !string.IsNullOrWhiteSpace(x.Timezone));

    RuleFor(x => x.AccessMode)
      .Must(v => v is "OPEN" or "RESTRICTED")
      .WithMessage("AccessMode must be OPEN or RESTRICTED");

    RuleFor(x => x.FullBehavior)
      .Must(v => v is "BLOCK" or "ALLOW_OVERFLOW")
      .WithMessage("FullBehavior must be BLOCK or ALLOW_OVERFLOW");
  }
}
