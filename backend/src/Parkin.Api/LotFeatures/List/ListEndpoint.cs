using FastEndpoints;
using FluentValidation;
using Parkin.Api.Authorization;

namespace Parkin.Api.LotFeatures.List;

public sealed class ListLotsRequest
{
  [BindFrom("page")]
  public int Page { get; init; } = 1;

  [BindFrom("per_page")]
  public int PerPage { get; init; } = Constants.DEFAULT_PAGE_SIZE;

  [BindFrom("status")]
  public LotStatusFilter? Status { get; init; }
}

public record LotListResponse : PagedResult<LotRecord>
{
  public LotListResponse(IReadOnlyList<LotRecord> Items, int Page, int PerPage, int TotalCount, int TotalPages)
    : base(Items, Page, PerPage, TotalCount, TotalPages)
  {
  }
}

public class ListEndpoint(IMediator mediator) : Endpoint<ListLotsRequest, LotListResponse, ListLotsMapper>
{
  private readonly IMediator _mediator = mediator;

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

      s.Responses[200] = "Paginated list of lots returned successfully";
      s.Responses[400] = "Invalid pagination parameters";
    });

    Tags("Lots");

    Description(builder => builder
      .Accepts<ListLotsRequest>()
      .Produces<LotListResponse>(200, "application/json")
      .ProducesProblem(400));
  }

  public override async Task HandleAsync(ListLotsRequest request, CancellationToken cancellationToken)
  {
    var result = await _mediator.Send(new ListLotsQuery(request.Page, request.PerPage, request.Status), cancellationToken);
    if (!result.IsSuccess)
    {
      await Send.ErrorsAsync(statusCode: 400, cancellationToken);
      return;
    }

    var pagedResult = result.Value;
    AddLinkHeader(pagedResult.Page, pagedResult.PerPage, pagedResult.TotalPages);

    var response = Map.FromEntity(pagedResult);
    await Send.OkAsync(response, cancellationToken);
  }

  private void AddLinkHeader(int page, int perPage, int totalPages)
  {
    var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
    string Link(string rel, int p) => $"<{baseUrl}?page={p}&per_page={perPage}>; rel=\"{rel}\"";

    var parts = new List<string>();
    if (page > 1)
    {
      parts.Add(Link("first", 1));
      parts.Add(Link("prev", page - 1));
    }
    if (page < totalPages)
    {
      parts.Add(Link("next", page + 1));
      parts.Add(Link("last", totalPages));
    }

    if (parts.Count > 0)
      HttpContext.Response.Headers["Link"] = string.Join(", ", parts);
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

  }
}

public sealed class ListLotsMapper
  : Mapper<ListLotsRequest, LotListResponse, PagedResult<LotDto>>
{
  public override LotListResponse FromEntity(PagedResult<LotDto> e)
  {
    var items = e.Items.Select(LotEnumMapping.ToRecord).ToList();

    return new LotListResponse(items, e.Page, e.PerPage, e.TotalCount, e.TotalPages);
  }
}
