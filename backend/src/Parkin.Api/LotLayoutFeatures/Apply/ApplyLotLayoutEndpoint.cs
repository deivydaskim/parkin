using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Extensions;
using Parkin.Api.LotFeatures;
using Parkin.Api.LotLayoutFeatures.Get;
using Parkin.Api.SpaceFeatures;

namespace Parkin.Api.LotLayoutFeatures.Apply;

public sealed class ApplyLotLayoutRequest
{
  public const string Route = "/lots/{LotId}/layout";
  public const int MaxSpacesPerBatch = 2000;

  public Guid LotId { get; init; }
  public LotLayoutRequest? Layout { get; init; }
  public bool ClearLayout { get; init; }
  public List<SpaceLayoutChangeRequest> Spaces { get; init; } = [];
}

public sealed class SpaceLayoutChangeRequest
{
  public Guid SpaceId { get; init; }
  public SpacePlacementRequest? Placement { get; init; }
  public string? Zone { get; init; }
}

public class ApplyLotLayoutEndpoint(IMediator mediator)
  : Endpoint<ApplyLotLayoutRequest, Results<Ok<LotLayoutViewRecord>, ValidationProblem, NotFound, ProblemHttpResult>>
{
  public override void Configure()
  {
    Put(ApplyLotLayoutRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Apply a lot layout in one batch";
      s.Description = "Atomically sets the lot footprint (omit to keep it, clearLayout=true to remove it) and the " +
        "placement of any number of its spaces. A row with placement=null un-places that space; zone is left " +
        "unchanged when omitted and cleared when empty. Either every row is applied or none is, and the batch " +
        "writes a single audit entry.";
      s.Responses[200] = "Layout applied; the updated layout projection is returned";
      s.Responses[400] = "Invalid rows: foreign or duplicate space IDs, or placements outside the footprint";
      s.Responses[404] = "Lot with specified ID not found";
    });

    Tags("Layout");

    Description(builder => builder
      .Accepts<ApplyLotLayoutRequest>()
      .Produces<LotLayoutViewRecord>(200, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<LotLayoutViewRecord>, ValidationProblem, NotFound, ProblemHttpResult>>
    ExecuteAsync(ApplyLotLayoutRequest request, CancellationToken cancellationToken)
  {
    var actorIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var actorId = actorIdClaim is null ? (Guid?)null : Guid.Parse(actorIdClaim);

    var errors = new List<ValidationError>();

    var layout = request.Layout?.ToValue();
    if (layout is { IsSuccess: false }) errors.AddRange(layout.ValidationErrors);

    var changes = new List<SpaceLayoutChange>(request.Spaces.Count);
    foreach (var row in request.Spaces)
    {
      var placement = row.Placement?.ToValue();
      if (placement is { IsSuccess: false })
      {
        errors.AddRange(placement.ValidationErrors);
        continue;
      }

      changes.Add(new SpaceLayoutChange(ParkingSpaceId.From(row.SpaceId), placement?.Value, row.Zone));
    }

    if (errors.Count > 0)
    {
      return Result<LotLayoutViewDto>.Invalid(errors).ToOkOrConflictResult(LotLayoutMapping.ToRecord);
    }

    var lotId = ParkingLotId.From(request.LotId);
    var applyResult = await mediator.Send(
      new ApplyLotLayoutCommand(lotId, layout?.Value, request.ClearLayout, changes, actorId), cancellationToken);

    if (!applyResult.IsSuccess)
    {
      var failure = applyResult.Status == ResultStatus.NotFound
        ? Result<LotLayoutViewDto>.NotFound()
        : Result<LotLayoutViewDto>.Invalid(applyResult.ValidationErrors);
      return failure.ToOkOrConflictResult(LotLayoutMapping.ToRecord);
    }

    var view = await mediator.Send(new GetLotLayoutQuery(lotId), cancellationToken);
    return view.ToOkOrConflictResult(LotLayoutMapping.ToRecord);
  }
}

public sealed class ApplyLotLayoutValidator : Validator<ApplyLotLayoutRequest>
{
  public ApplyLotLayoutValidator()
  {
    RuleFor(x => x.LotId)
      .NotEmpty()
      .WithMessage("Lot ID is required");

    RuleFor(x => x.Layout!)
      .SetValidator(new LotLayoutRequestValidator())
      .When(x => x.Layout is not null);

    RuleFor(x => x.ClearLayout)
      .Equal(false)
      .WithMessage("Send either layout or clearLayout, not both")
      .When(x => x.Layout is not null);

    RuleFor(x => x.Spaces)
      .NotNull()
      .Must(spaces => spaces.Count <= ApplyLotLayoutRequest.MaxSpacesPerBatch)
      .WithMessage($"A batch may contain at most {ApplyLotLayoutRequest.MaxSpacesPerBatch} spaces");

    RuleForEach(x => x.Spaces).ChildRules(row =>
    {
      row.RuleFor(r => r.SpaceId)
        .NotEmpty()
        .WithMessage("Space ID is required");

      row.RuleFor(r => r.Zone)
        .MaximumLength(ParkingSpace.ZoneMaxLength)
        .WithMessage($"Zone must not exceed {ParkingSpace.ZoneMaxLength} characters");

      row.RuleFor(r => r.Placement!)
        .SetValidator(new SpacePlacementRequestValidator())
        .When(r => r.Placement is not null);
    });
  }
}
