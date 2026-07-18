using FastEndpoints;
using FluentValidation;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.GrantFeatures.List;

public sealed class ListGrantsByDriverRequest
{
  public const string Route = "/drivers/{DriverId}/grants";

  public Guid DriverId { get; init; }

  [BindFrom("page")]
  public int Page { get; init; } = 1;

  [BindFrom("per_page")]
  public int PerPage { get; init; } = Constants.DEFAULT_PAGE_SIZE;
}

public record GrantListResponse : PagedResult<GrantRecord>
{
  public GrantListResponse(IReadOnlyList<GrantRecord> Items, int Page, int PerPage, int TotalCount, int TotalPages)
    : base(Items, Page, PerPage, TotalCount, TotalPages)
  {
  }
}

public class ListGrantsByDriverEndpoint(IMediator mediator)
  : Endpoint<ListGrantsByDriverRequest, GrantListResponse, ListGrantsByDriverMapper>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Get(ListGrantsByDriverRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "List a driver's access grants with pagination";
      s.Description = "Retrieves a paginated list of access grants belonging to a driver.";
      s.Params["page"] = "1-based page index (default 1)";
      s.Params["per_page"] = $"Page size 1–{Constants.MAX_PAGE_SIZE} (default {Constants.DEFAULT_PAGE_SIZE})";

      s.Responses[200] = "Paginated list of grants returned successfully";
      s.Responses[400] = "Invalid pagination parameters";
    });

    Tags("Grants");

    Description(builder => builder
      .Accepts<ListGrantsByDriverRequest>()
      .Produces<GrantListResponse>(200, "application/json")
      .ProducesProblem(400));
  }

  public override async Task HandleAsync(ListGrantsByDriverRequest request, CancellationToken cancellationToken)
  {
    var result = await _mediator.Send(
      new ListGrantsByDriverQuery(DriverId.From(request.DriverId), request.Page, request.PerPage), cancellationToken);
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

public sealed class ListGrantsByDriverValidator : Validator<ListGrantsByDriverRequest>
{
  public ListGrantsByDriverValidator()
  {
    RuleFor(x => x.DriverId)
      .NotEmpty()
      .WithMessage("Driver ID is required");

    RuleFor(x => x.Page)
      .GreaterThanOrEqualTo(1)
      .WithMessage("page must be >= 1");

    RuleFor(x => x.PerPage)
      .InclusiveBetween(1, Constants.MAX_PAGE_SIZE)
      .WithMessage($"per_page must be between 1 and {Constants.MAX_PAGE_SIZE}");
  }
}

public sealed class ListGrantsByDriverMapper
  : Mapper<ListGrantsByDriverRequest, GrantListResponse, PagedResult<GrantDto>>
{
  public override GrantListResponse FromEntity(PagedResult<GrantDto> e)
  {
    var items = e.Items.Select(GrantMapping.ToRecord).ToList();

    return new GrantListResponse(items, e.Page, e.PerPage, e.TotalCount, e.TotalPages);
  }
}
