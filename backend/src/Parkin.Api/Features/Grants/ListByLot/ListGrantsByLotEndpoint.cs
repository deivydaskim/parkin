using FastEndpoints;
using FluentValidation;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Features.Grants.List;

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
  : Endpoint<ListGrantsByLotRequest, GrantListResponse, ListGrantsByLotMapper>
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

  public override async Task HandleAsync(ListGrantsByLotRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
      new ListGrantsByLotQuery(ParkingLotId.From(request.LotId), request.Page, request.PerPage), cancellationToken);
    if (!result.IsSuccess)
    {
      await Send.ErrorsAsync(statusCode: 400, cancellationToken);
      return;
    }

    var pagedResult = result.Value;
    AddLinkHeader(pagedResult.Page, pagedResult.PerPage, pagedResult.TotalPages);

    await Send.OkAsync(Map.FromEntity(pagedResult), cancellationToken);
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

public sealed class ListGrantsByLotMapper
  : Mapper<ListGrantsByLotRequest, GrantListResponse, PagedResult<GrantDto>>
{
  public override GrantListResponse FromEntity(PagedResult<GrantDto> e)
  {
    var items = e.Items.Select(GrantMapping.ToRecord).ToList();

    return new GrantListResponse(items, e.Page, e.PerPage, e.TotalCount, e.TotalPages);
  }
}
