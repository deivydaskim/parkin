using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Grants.ListByLot;

public sealed class ListGrantsByLotRequest
{
  public const string Route = "/lots/{LotId}/grants";

  public Guid LotId { get; init; }

  [BindFrom("page")]
  public int Page { get; init; } = 1;

  [BindFrom("per_page")]
  public int PerPage { get; init; } = Constants.DEFAULT_PAGE_SIZE;
}

public class ListGrantsByLotEndpoint(IMediator mediator)
  : Endpoint<ListGrantsByLotRequest, Results<Ok<GrantListResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Get(ListGrantsByLotRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "List a lot's access grants with pagination";
      s.Description = "Retrieves a paginated list of access grants issued for a lot, including driver names.";
      s.Params["page"] = "1-based page index (default 1)";
      s.Params["per_page"] = $"Page size 1–{Constants.MAX_PAGE_SIZE} (default {Constants.DEFAULT_PAGE_SIZE})";

      s.Responses[200] = "Paginated list of grants returned successfully";
      s.Responses[400] = "Invalid pagination parameters";
    });

    Tags("Grants");

    Description(builder => builder
      .Accepts<ListGrantsByLotRequest>()
      .Produces<GrantListResponse>(200, "application/json")
      .ProducesProblem(400));
  }

  public override async Task<Results<Ok<GrantListResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(ListGrantsByLotRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
      new ListGrantsByLotQuery(ParkingLotId.From(request.LotId), request.Page, request.PerPage), cancellationToken);

    if (result.IsSuccess) HttpContext.AppendPaginationLinks(result.Value);

    return result.ToOkResult(GrantListResponse.From);
  }
}

public sealed class ListGrantsByLotValidator : Validator<ListGrantsByLotRequest>
{
  public ListGrantsByLotValidator()
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
