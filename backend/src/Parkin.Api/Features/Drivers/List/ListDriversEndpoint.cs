using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Drivers.List;

public sealed class ListDriversRequest
{
  public const string Route = "/drivers";

  [BindFrom("page")]
  public int Page { get; init; } = 1;

  [BindFrom("per_page")]
  public int PerPage { get; init; } = Constants.DEFAULT_PAGE_SIZE;

  [BindFrom("status")]
  public DriverStatusFilter? Status { get; init; }

  [BindFrom("search")]
  public string? Search { get; init; }
}

public class ListDriversEndpoint(IMediator mediator)
  : Endpoint<ListDriversRequest, Results<Ok<DriverListResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Get(ListDriversRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "List drivers with pagination";
      s.Description = "Retrieves a paginated list of driver records.";
      s.Params["page"] = "1-based page index (default 1)";
      s.Params["per_page"] = $"Page size 1–{Constants.MAX_PAGE_SIZE} (default {Constants.DEFAULT_PAGE_SIZE})";
      s.Params["status"] = "Active (default), Archived, or All";
      s.Params["search"] = "Case-insensitive match on name, contact, or any plate number";

      s.Responses[200] = "Paginated list of drivers returned successfully";
      s.Responses[400] = "Invalid pagination parameters";
    });

    Tags("Drivers");

    Description(builder => builder
      .Accepts<ListDriversRequest>()
      .Produces<DriverListResponse>(200, "application/json")
      .ProducesProblem(400));
  }

  public override async Task<Results<Ok<DriverListResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(ListDriversRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
      new ListDriversQuery(request.Page, request.PerPage, request.Status, request.Search), cancellationToken);

    if (result.IsSuccess) HttpContext.AppendPaginationLinks(result.Value);

    return result.ToOkResult(DriverListResponse.From);
  }
}

public sealed class ListDriversValidator : Validator<ListDriversRequest>
{
  public ListDriversValidator()
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
