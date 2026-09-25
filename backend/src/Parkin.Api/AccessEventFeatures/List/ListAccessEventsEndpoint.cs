using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.AccessEventFeatures.List;

public sealed class ListAccessEventsRequest
{
  public const string Route = "/lots/{LotId}/access-events";

  public Guid LotId { get; init; }

  [BindFrom("page")]
  public int Page { get; init; } = 1;

  [BindFrom("per_page")]
  public int PerPage { get; init; } = Constants.DEFAULT_PAGE_SIZE;
}

public record AccessEventListResponse : PagedResult<AccessEventListItemRecord>
{
  public AccessEventListResponse(IReadOnlyList<AccessEventListItemRecord> Items, int Page, int PerPage, int TotalCount,
    int TotalPages)
    : base(Items, Page, PerPage, TotalCount, TotalPages)
  {
  }
}

public class ListAccessEventsEndpoint(IMediator mediator)
  : Endpoint<ListAccessEventsRequest, Results<Ok<AccessEventListResponse>, NotFound>>
{
  public override void Configure()
  {
    Get(ListAccessEventsRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "List a lot's recent access events, newest first";
      s.Description = "Read-only feed of gate and manual entry/exit decisions for a lot: plate, direction, " +
        "decision and deny reason, the pool or reserved space taken, the source, and the matched driver.";
      s.Params["page"] = "1-based page index (default 1)";
      s.Params["per_page"] = $"Page size 1–{Constants.MAX_PAGE_SIZE} (default {Constants.DEFAULT_PAGE_SIZE})";
      s.Responses[200] = "Paginated list of access events returned successfully";
      s.Responses[400] = "Invalid pagination parameters";
      s.Responses[404] = "Lot with specified ID not found";
    });

    Tags("AccessEvents");

    Description(builder => builder
      .Accepts<ListAccessEventsRequest>()
      .Produces<AccessEventListResponse>(200, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<AccessEventListResponse>, NotFound>>
    ExecuteAsync(ListAccessEventsRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
      new ListAccessEventsQuery(ParkingLotId.From(request.LotId), request.Page, request.PerPage), cancellationToken);

    if (result.Status == ResultStatus.NotFound) return TypedResults.NotFound();

    var paged = result.Value;
    AddLinkHeader(paged.Page, paged.PerPage, paged.TotalPages);

    var items = paged.Items.Select(AccessEventListItemRecord.FromDto).ToList();
    return TypedResults.Ok(new AccessEventListResponse(items, paged.Page, paged.PerPage, paged.TotalCount,
      paged.TotalPages));
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

public sealed class ListAccessEventsValidator : Validator<ListAccessEventsRequest>
{
  public ListAccessEventsValidator()
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
