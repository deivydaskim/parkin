using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Users.List;

public sealed class ListUsersRequest
{
  public const string Route = "/users";

  [BindFrom("page")]
  public int Page { get; init; } = 1;

  [BindFrom("per_page")]
  public int PerPage { get; init; } = Constants.DEFAULT_PAGE_SIZE;
}

public record UserListResponse : PagedResult<UserResponse>
{
  public UserListResponse(IReadOnlyList<UserResponse> Items, int Page, int PerPage, int TotalCount, int TotalPages)
    : base(Items, Page, PerPage, TotalCount, TotalPages)
  {
  }

  public static UserListResponse From(PagedResult<UserResponse> page)
    => new(page.Items, page.Page, page.PerPage, page.TotalCount, page.TotalPages);
}

public class ListUsersEndpoint(IMediator mediator)
  : Endpoint<ListUsersRequest, Results<Ok<UserListResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Get(ListUsersRequest.Route);
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

  public override async Task<Results<Ok<UserListResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(ListUsersRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new ListUsersQuery(request.Page, request.PerPage), cancellationToken);
    return result.ToHttpResult(page =>
    {
      HttpContext.AppendPaginationLinks(page);
      return TypedResults.Ok(UserListResponse.From(page));
    });
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
