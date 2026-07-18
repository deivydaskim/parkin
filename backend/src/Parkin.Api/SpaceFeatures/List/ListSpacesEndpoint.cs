using FastEndpoints;
using FluentValidation;
using Parkin.Api.Authorization;

namespace Parkin.Api.SpaceFeatures.List;

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
}

public record SpaceListResponse : PagedResult<SpaceRecord>
{
  public SpaceListResponse(IReadOnlyList<SpaceRecord> Items, int Page, int PerPage, int TotalCount, int TotalPages)
    : base(Items, Page, PerPage, TotalCount, TotalPages)
  {
  }
}

public class ListSpacesEndpoint(IMediator mediator) : Endpoint<ListSpacesRequest, SpaceListResponse, ListSpacesMapper>
{
  private readonly IMediator _mediator = mediator;

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

      s.Responses[200] = "Paginated list of spaces returned successfully";
      s.Responses[400] = "Invalid pagination parameters";
    });

    Tags("Spaces");

    Description(builder => builder
      .Accepts<ListSpacesRequest>()
      .Produces<SpaceListResponse>(200, "application/json")
      .ProducesProblem(400));
  }

  public override async Task HandleAsync(ListSpacesRequest request, CancellationToken cancellationToken)
  {
    var result = await _mediator.Send(new ListSpacesQuery(request.LotId, request.Page, request.PerPage, request.Status), cancellationToken);
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

  }
}

public sealed class ListSpacesMapper
  : Mapper<ListSpacesRequest, SpaceListResponse, PagedResult<SpaceDto>>
{
  public override SpaceListResponse FromEntity(PagedResult<SpaceDto> e)
  {
    var items = e.Items.Select(SpaceEnumMapping.ToRecord).ToList();

    return new SpaceListResponse(items, e.Page, e.PerPage, e.TotalCount, e.TotalPages);
  }
}
