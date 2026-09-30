using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Features.Lots;
using Parkin.Api.Features.Spaces;
using Parkin.Api.Web;

namespace Parkin.Api.Features.LotLayouts.Apply;

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

  public SpaceLayoutChange ToValue() =>
    new(ParkingSpaceId.From(SpaceId), Placement?.ToValue().Value, Zone);
}

public class ApplyLotLayoutEndpoint(IMediator mediator, ICurrentUser currentUser)
  : Endpoint<ApplyLotLayoutRequest, Results<Ok<LotLayoutViewResponse>, ValidationProblem, ProblemHttpResult>>
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
      .Produces<LotLayoutViewResponse>(200, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<LotLayoutViewResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(ApplyLotLayoutRequest request, CancellationToken cancellationToken)
  {
    var command = new ApplyLotLayoutCommand(
      ParkingLotId.From(request.LotId),
      request.Layout?.ToValue().Value,
      request.ClearLayout,
      request.Spaces.Select(row => row.ToValue()).ToList(),
      currentUser.Id);

    var result = await mediator.Send(command, cancellationToken);

    return result.ToOkResult(view => view);
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

      row.RuleFor(r => r.Placement!)
        .SetValidator(new SpacePlacementRequestValidator())
        .When(r => r.Placement is not null);
    });
  }
}
