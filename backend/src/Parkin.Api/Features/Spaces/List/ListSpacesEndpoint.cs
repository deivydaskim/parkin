using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Spaces.List;

public sealed class ListSpacesRequest
{
  public const string Route = "/lots/{LotId}/spaces";

  public Guid LotId { get; init; }

  [BindFrom("page")]
  public int Page { get; init; } = 1;

  [BindFrom("per_page")]
  public int PerPage { get; init; } = Constants.DEFAULT_PAGE_SIZE;

  [BindFrom("status")]
  public SpaceStatusFilter? Status { get; init; }

  [BindFrom("type")]
  public SpaceType? Type { get; init; }

  [BindFrom("search")]
  public string? Search { get; init; }
}

public record SpaceListResponse(
  IReadOnlyList<SpaceResponse> Items, int Page, int PerPage, int TotalCount, int TotalPages)
  : PagedResult<SpaceResponse>(Items, Page, PerPage, TotalCount, TotalPages)
{
  public static SpaceListResponse From(PagedResult<SpaceResponse> page) =>
    new(page.Items, page.Page, page.PerPage, page.TotalCount, page.TotalPages);
}

public class ListSpacesEndpoint(IMediator mediator)
  : Endpoint<ListSpacesRequest, Results<Ok<SpaceListResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Get(ListSpacesRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "List parking spaces for a lot, with pagination";
      s.Description = "Retrieves a paginated list of parking spaces for a lot. Defaults to active-only; pass status=Inactive or status=All to include inactive spaces.";
      s.Params["page"] = "1-based page index (default 1)";
      s.Params["per_page"] = $"Page size 1–{Constants.MAX_PAGE_SIZE} (default {Constants.DEFAULT_PAGE_SIZE})";
      s.Params["status"] = "Active (default), Inactive, or All";
      s.Params["type"] = "Only General or only Reserved spaces (default: both)";
      s.Params["search"] = "Case-insensitive match on the space label";

      s.Responses[200] = "Paginated list of spaces returned successfully";
      s.Responses[400] = "Invalid pagination parameters";
    });

    Tags("Spaces");

    Description(builder => builder
      .Accepts<ListSpacesRequest>()
      .Produces<SpaceListResponse>(200, "application/json")
      .ProducesProblem(400));
  }

  public override async Task<Results<Ok<SpaceListResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(ListSpacesRequest request, CancellationToken cancellationToken)
  {
    var query = new ListSpacesQuery(ParkingLotId.From(request.LotId), request.Page, request.PerPage, request.Status,
      request.Type, request.Search);
    var result = await mediator.Send(query, cancellationToken);

    return result.ToHttpResult(page =>
    {
      HttpContext.AppendPaginationLinks(page);
      return TypedResults.Ok(SpaceListResponse.From(page));
    });
  }
}

public sealed class ListSpacesValidator : Validator<ListSpacesRequest>
{
  public ListSpacesValidator()
  {
    RuleFor(x => x.LotId)
      .NotEmpty()
      .WithMessage("Lot ID is required");

    RuleFor(x => x.Page)
      .GreaterThanOrEqualTo(1)
      .WithMessage("page must be >= 1");

    RuleFor(x => x.PerPage)
      .InclusiveBetween(1, Constants.MAX_PAGE_SIZE)
      .WithMessage($"per_page must be between 1 and {Constants.MAX_PAGE_SIZE}");

    RuleFor(x => x.Search)
      .MaximumLength(ParkingSpace.LabelMaxLength)
      .WithMessage($"search must not exceed {ParkingSpace.LabelMaxLength} characters");
  }
}
