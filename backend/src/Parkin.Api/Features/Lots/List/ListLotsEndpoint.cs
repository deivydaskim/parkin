using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Lots.List;

public sealed class ListLotsRequest
{
  [BindFrom("page")]
  public int Page { get; init; } = 1;

  [BindFrom("per_page")]
  public int PerPage { get; init; } = Constants.DEFAULT_PAGE_SIZE;

  [BindFrom("status")]
  public LotStatusFilter? Status { get; init; }

  [BindFrom("search")]
  public string? Search { get; init; }
}

public record LotListResponse(IReadOnlyList<LotResponse> Items, int Page, int PerPage, int TotalCount, int TotalPages)
  : PagedResult<LotResponse>(Items, Page, PerPage, TotalCount, TotalPages)
{
  public static LotListResponse From(PagedResult<LotResponse> page) =>
    new(page.Items, page.Page, page.PerPage, page.TotalCount, page.TotalPages);
}

public class ListLotsEndpoint(IMediator mediator)
  : Endpoint<ListLotsRequest, Results<Ok<LotListResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Get("/lots");
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "List parking lots with pagination";
      s.Description = "Retrieves a paginated list of parking lots. Defaults to active-only; pass status=Archived or status=All to include archived lots.";
      s.Params["page"] = "1-based page index (default 1)";
      s.Params["per_page"] = $"Page size 1–{Constants.MAX_PAGE_SIZE} (default {Constants.DEFAULT_PAGE_SIZE})";
      s.Params["status"] = "Active (default), Archived, or All";
      s.Params["search"] = "Case-insensitive match on name or address";

      s.Responses[200] = "Paginated list of lots returned successfully";
      s.Responses[400] = "Invalid pagination parameters";
    });

    Tags("Lots");

    Description(builder => builder
      .Accepts<ListLotsRequest>()
      .Produces<LotListResponse>(200, "application/json")
      .ProducesProblem(400));
  }

  public override async Task<Results<Ok<LotListResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(ListLotsRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
      new ListLotsQuery(request.Page, request.PerPage, request.Status, request.Search), cancellationToken);

    return result.ToHttpResult(page =>
    {
      HttpContext.AppendPaginationLinks(page);
      return TypedResults.Ok(LotListResponse.From(page));
    });
  }
}

public sealed class ListLotsValidator : Validator<ListLotsRequest>
{
  public ListLotsValidator()
  {
    RuleFor(x => x.Page)
      .GreaterThanOrEqualTo(1)
      .WithMessage("page must be >= 1");

    RuleFor(x => x.PerPage)
      .InclusiveBetween(1, Constants.MAX_PAGE_SIZE)
      .WithMessage($"per_page must be between 1 and {Constants.MAX_PAGE_SIZE}");

    RuleFor(x => x.Search)
      .MaximumLength(200)
      .WithMessage("search must not exceed 200 characters");
  }
}
