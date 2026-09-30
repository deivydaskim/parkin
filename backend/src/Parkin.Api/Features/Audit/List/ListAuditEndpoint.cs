using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Audit.List;

public sealed class ListAuditRequest
{
  public const string Route = "/audit";

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

public record AuditListResponse : PagedResult<AuditLogEntryResponse>
{
  public AuditListResponse(IReadOnlyList<AuditLogEntryResponse> Items, int Page, int PerPage, int TotalCount, int TotalPages)
    : base(Items, Page, PerPage, TotalCount, TotalPages)
  {
  }

  public static AuditListResponse From(PagedResult<AuditLogEntryResponse> page)
    => new(page.Items, page.Page, page.PerPage, page.TotalCount, page.TotalPages);
}

public class ListAuditEndpoint(IMediator mediator)
  : Endpoint<ListAuditRequest, Results<Ok<AuditListResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Get(ListAuditRequest.Route);
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

  public override async Task<Results<Ok<AuditListResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(ListAuditRequest request, CancellationToken cancellationToken)
  {
    var filter = new AuditLogFilter(request.Page, request.PerPage, request.From, request.To, request.Actor,
      request.ActorType, request.Entity);
    var result = await mediator.Send(new ListAuditQuery(filter), cancellationToken);

    return result.ToHttpResult(page =>
    {
      HttpContext.AppendPaginationLinks(page);
      return TypedResults.Ok(AuditListResponse.From(page));
    });
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
