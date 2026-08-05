using FastEndpoints;
using FluentValidation;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.AuditFeatures.List;

public sealed class ListAuditRequest
{
  [BindFrom("page")]
  public int Page { get; init; } = 1;

  [BindFrom("per_page")]
  public int PerPage { get; init; } = Constants.DEFAULT_PAGE_SIZE;

  [BindFrom("from")]
  public DateTimeOffset? From { get; init; }

  [BindFrom("to")]
  public DateTimeOffset? To { get; init; }

  [BindFrom("actor")]
  public Guid? Actor { get; init; }

  [BindFrom("actor_type")]
  public AuditActorType? ActorType { get; init; }

  [BindFrom("entity")]
  public string? Entity { get; init; }
}

public record AuditListResponse : PagedResult<AuditLogEntryRecord>
{
  public AuditListResponse(IReadOnlyList<AuditLogEntryRecord> Items, int Page, int PerPage, int TotalCount, int TotalPages)
    : base(Items, Page, PerPage, TotalCount, TotalPages)
  {
  }
}

public class ListEndpoint(IMediator mediator) : Endpoint<ListAuditRequest, AuditListResponse>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Get("/audit");
    Roles(AccessPolicies.AdminOnly);

    Summary(s =>
    {
      s.Summary = "Query the audit log";
      s.Description = "Retrieves a paginated, newest-first view of the append-only audit log. Filterable by date range, " +
                       "actor, and entity type. Includes access decisions and every admin mutation.";
      s.Params["page"] = "1-based page index (default 1)";
      s.Params["per_page"] = $"Page size 1–{Constants.MAX_PAGE_SIZE} (default {Constants.DEFAULT_PAGE_SIZE})";
      s.Params["from"] = "Only entries occurred at or after this instant (inclusive)";
      s.Params["to"] = "Only entries occurred at or before this instant (inclusive)";
      s.Params["actor"] = "Filter by the staff/system actor's ID";
      s.Params["actor_type"] = "Filter by actor type: Staff, System, or Api";
      s.Params["entity"] = "Filter by entity type, e.g. Reservation, ParkingLot, AccessGrant";

      s.Responses[200] = "Paginated list of audit entries returned successfully";
      s.Responses[400] = "Invalid pagination or filter parameters";
    });

    Tags("Audit");

    Description(builder => builder
      .Accepts<ListAuditRequest>()
      .Produces<AuditListResponse>(200, "application/json")
      .ProducesProblem(400));
  }

  public override async Task HandleAsync(ListAuditRequest request, CancellationToken cancellationToken)
  {
    var result = await _mediator.Send(
      new ListAuditQuery(request.Page, request.PerPage, request.From, request.To, request.Actor, request.ActorType, request.Entity),
      cancellationToken);

    if (!result.IsSuccess)
    {
      await Send.ErrorsAsync(statusCode: 400, cancellationToken);
      return;
    }

    var pagedResult = result.Value;
    AddLinkHeader(pagedResult.Page, pagedResult.PerPage, pagedResult.TotalPages);

    var items = pagedResult.Items.Select(AuditMapping.ToRecord).ToList();
    var response = new AuditListResponse(items, pagedResult.Page, pagedResult.PerPage, pagedResult.TotalCount, pagedResult.TotalPages);
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

public sealed class ListAuditValidator : Validator<ListAuditRequest>
{
  public ListAuditValidator()
  {
    RuleFor(x => x.Page)
      .GreaterThanOrEqualTo(1)
      .WithMessage("page must be >= 1");

    RuleFor(x => x.PerPage)
      .InclusiveBetween(1, Constants.MAX_PAGE_SIZE)
      .WithMessage($"per_page must be between 1 and {Constants.MAX_PAGE_SIZE}");

    RuleFor(x => x.To)
      .GreaterThanOrEqualTo(x => x.From)
      .When(x => x.From.HasValue && x.To.HasValue)
      .WithMessage("to must not be before from");

    RuleFor(x => x.Entity)
      .Must(entity => AuditEntityTypes.All.Contains(entity))
      .When(x => x.Entity is not null)
      .WithMessage($"entity must be one of: {string.Join(", ", AuditEntityTypes.All)}");
  }
}
