using FastEndpoints;
using FluentValidation;
using Parkin.Api.Authorization;

namespace Parkin.Api.UserFeatures.List;

public sealed class ListUsersRequest
{
  [BindFrom("page")]
  public int Page { get; init; } = 1;

  [BindFrom("per_page")]
  public int PerPage { get; init; } = Constants.DEFAULT_PAGE_SIZE;
}

public record UserListResponse : PagedResult<UserRecord>
{
  public UserListResponse(IReadOnlyList<UserRecord> Items, int Page, int PerPage, int TotalCount, int TotalPages)
    : base(Items, Page, PerPage, TotalCount, TotalPages)
  {
  }
}

public class ListEndpoint(IMediator mediator) : Endpoint<ListUsersRequest, UserListResponse>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Get("/users");
    Roles(AccessPolicies.AdminOnly);

    Summary(s =>
    {
      s.Summary = "List staff accounts with pagination";
      s.Description = "Retrieves a paginated list of staff accounts with their current role and status.";
      s.Params["page"] = "1-based page index (default 1)";
      s.Params["per_page"] = $"Page size 1–{Constants.MAX_PAGE_SIZE} (default {Constants.DEFAULT_PAGE_SIZE})";

      s.Responses[200] = "Paginated list of users returned successfully";
      s.Responses[400] = "Invalid pagination parameters";
    });

    Tags("Users");

    Description(builder => builder
      .Accepts<ListUsersRequest>()
      .Produces<UserListResponse>(200, "application/json")
      .ProducesProblem(400));
  }

  public override async Task HandleAsync(ListUsersRequest request, CancellationToken cancellationToken)
  {
    var result = await _mediator.Send(new ListUsersQuery(request.Page, request.PerPage), cancellationToken);
    if (!result.IsSuccess)
    {
      await Send.ErrorsAsync(statusCode: 400, cancellationToken);
      return;
    }

    var pagedResult = result.Value;
    AddLinkHeader(pagedResult.Page, pagedResult.PerPage, pagedResult.TotalPages);

    var response = new UserListResponse(pagedResult.Items, pagedResult.Page, pagedResult.PerPage,
      pagedResult.TotalCount, pagedResult.TotalPages);
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

public sealed class ListUsersValidator : Validator<ListUsersRequest>
{
  public ListUsersValidator()
  {
    RuleFor(x => x.Page)
      .GreaterThanOrEqualTo(1)
      .WithMessage("page must be >= 1");

    RuleFor(x => x.PerPage)
      .InclusiveBetween(1, Constants.MAX_PAGE_SIZE)
      .WithMessage($"per_page must be between 1 and {Constants.MAX_PAGE_SIZE}");
  }
}
