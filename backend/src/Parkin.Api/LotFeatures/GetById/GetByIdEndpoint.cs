using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Extensions;

namespace Parkin.Api.LotFeatures.GetById;

public sealed class GetLotByIdRequest
{
  public const string Route = "/lots/{LotId}";
  public Guid LotId { get; init; }
}

public class GetByIdEndpoint(IMediator mediator)
  : Endpoint<GetLotByIdRequest, Results<Ok<LotRecord>, NotFound, ProblemHttpResult>>
{
  public override void Configure()
  {
    Get(GetLotByIdRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Get a parking lot by ID";
      s.Description = "Retrieves a specific parking lot by its unique identifier, regardless of its status (active or archived).";
      s.Responses[200] = "Lot found and returned successfully";
      s.Responses[404] = "Lot with specified ID not found";
    });

    Tags("Lots");

    Description(builder => builder
      .Accepts<GetLotByIdRequest>()
      .Produces<LotRecord>(200, "application/json")
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<LotRecord>, NotFound, ProblemHttpResult>>
    ExecuteAsync(GetLotByIdRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new GetLotQuery(ParkingLotId.From(request.LotId)), cancellationToken);

    return result.ToGetByIdResult(LotEnumMapping.ToRecord);
  }
}

public sealed class GetLotByIdValidator : Validator<GetLotByIdRequest>
{
  public GetLotByIdValidator()
  {
    RuleFor(x => x.LotId)
      .NotEmpty()
      .WithMessage("Lot ID is required");
  }
}
