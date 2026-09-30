using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Lots.Archive;

public sealed class ArchiveLotRequest
{
  public const string Route = "/lots/{LotId}/archive";
  public Guid LotId { get; init; }
}

public class ArchiveLotEndpoint(IMediator mediator, ICurrentUser currentUser)
  : Endpoint<ArchiveLotRequest, Results<Ok<LotResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(ArchiveLotRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Archive a parking lot";
      s.Description = "Archives a parking lot. Archived lots are hidden from the default operational list but remain directly readable by ID.";
      s.Responses[200] = "Lot archived successfully";
      s.Responses[404] = "Lot with specified ID not found";
    });

    Tags("Lots");

    Description(builder => builder
      .Accepts<ArchiveLotRequest>()
      .Produces<LotResponse>(200, "application/json")
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<LotResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(ArchiveLotRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
      new ArchiveLotCommand(ParkingLotId.From(request.LotId), currentUser.Id), cancellationToken);

    return result.ToOkResult(lot => lot);
  }
}

public sealed class ArchiveLotValidator : Validator<ArchiveLotRequest>
{
  public ArchiveLotValidator()
  {
    RuleFor(x => x.LotId)
      .NotEmpty()
      .WithMessage("Lot ID is required");
  }
}
