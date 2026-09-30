using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Sessions.ListActiveByLot;

public sealed class ListActiveSessionsByLotRequest
{
  public const string Route = "/lots/{LotId}/active-sessions";

  public Guid LotId { get; init; }

  [BindFrom("page")]
  public int Page { get; init; } = 1;

  [BindFrom("per_page")]
  public int PerPage { get; init; } = Constants.DEFAULT_PAGE_SIZE;
}

public record ActiveSessionListResponse : PagedResult<ActiveSessionResponse>
{
  public ActiveSessionListResponse(IReadOnlyList<ActiveSessionResponse> Items, int Page, int PerPage, int TotalCount, int TotalPages)
    : base(Items, Page, PerPage, TotalCount, TotalPages)
  {
  }

  public static ActiveSessionListResponse From(PagedResult<ActiveSessionResponse> page)
    => new(page.Items, page.Page, page.PerPage, page.TotalCount, page.TotalPages);
}

public class ListActiveSessionsByLotEndpoint(IMediator mediator)
  : Endpoint<ListActiveSessionsByLotRequest,
    Results<Ok<ActiveSessionListResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Get(ListActiveSessionsByLotRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "List vehicles currently parked in a lot";
      s.Description = "Retrieves a paginated list of active parking sessions for a lot, newest entry first.";
      s.Params["page"] = "1-based page index (default 1)";
      s.Params["per_page"] = $"Page size 1–{Constants.MAX_PAGE_SIZE} (default {Constants.DEFAULT_PAGE_SIZE})";

      s.Responses[200] = "Paginated list of active sessions returned successfully";
      s.Responses[400] = "Invalid pagination parameters";
    });

    Tags("Sessions");

    Description(builder => builder
      .Accepts<ListActiveSessionsByLotRequest>()
      .Produces<ActiveSessionListResponse>(200, "application/json")
      .ProducesProblem(400));
  }

  public override async Task<Results<Ok<ActiveSessionListResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(ListActiveSessionsByLotRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
      new ListActiveSessionsByLotQuery(ParkingLotId.From(request.LotId), request.Page, request.PerPage),
      cancellationToken);

    return result.ToHttpResult(page =>
    {
      HttpContext.AppendPaginationLinks(page);
      return TypedResults.Ok(ActiveSessionListResponse.From(page));
    });
  }
}

public sealed class ListActiveSessionsByLotValidator : Validator<ListActiveSessionsByLotRequest>
{
  public ListActiveSessionsByLotValidator()
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
