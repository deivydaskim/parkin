using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.AccessGrantAggregate;
using Parkin.Api.Extensions;

namespace Parkin.Api.GrantFeatures.Revoke;

public sealed class RevokeGrantRequest
{
  public const string Route = "/grants/{GrantId}/revoke";
  public Guid GrantId { get; init; }
}

public class RevokeGrantEndpoint(IMediator mediator)
  : Endpoint<RevokeGrantRequest, Results<Ok<GrantRecord>, NotFound, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(RevokeGrantRequest.Route);
    Roles(AccessPolicies.OperatorOrAbove);

    Summary(s =>
    {
      s.Summary = "Revoke an access grant";
      s.Description = "Revokes a driver's access grant to a lot. Takes effect on the next access event.";
      s.Responses[200] = "Grant revoked successfully";
      s.Responses[404] = "Grant with specified ID not found";
    });

    Tags("Grants");

    Description(builder => builder
      .Accepts<RevokeGrantRequest>()
      .Produces<GrantRecord>(200, "application/json")
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<GrantRecord>, NotFound, ProblemHttpResult>>
    ExecuteAsync(RevokeGrantRequest request, CancellationToken cancellationToken)
  {
    var actorIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var actorId = actorIdClaim is null ? (Guid?)null : Guid.Parse(actorIdClaim);
    var result = await mediator.Send(new RevokeGrantCommand(AccessGrantId.From(request.GrantId), actorId), cancellationToken);

    return result.ToUpdateResult(GrantMapping.ToRecord);
  }
}

public sealed class RevokeGrantValidator : Validator<RevokeGrantRequest>
{
  public RevokeGrantValidator()
  {
    RuleFor(x => x.GrantId)
      .NotEmpty()
      .WithMessage("Grant ID is required");
  }
}
