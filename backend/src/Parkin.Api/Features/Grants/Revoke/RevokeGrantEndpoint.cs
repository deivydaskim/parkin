using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.AccessGrantAggregate;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Grants.Revoke;

public sealed class RevokeGrantRequest
{
  public const string Route = "/grants/{GrantId}/revoke";
  public Guid GrantId { get; init; }
}

public class RevokeGrantEndpoint(IMediator mediator, ICurrentUser currentUser)
  : Endpoint<RevokeGrantRequest, Results<Ok<GrantResponse>, ValidationProblem, ProblemHttpResult>>
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
      s.Responses[400] = "Grant is already revoked";
      s.Responses[404] = "Grant with specified ID not found";
    });

    Tags("Grants");

    Description(builder => builder
      .Accepts<RevokeGrantRequest>()
      .Produces<GrantResponse>(200, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<GrantResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(RevokeGrantRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
      new RevokeGrantCommand(AccessGrantId.From(request.GrantId), currentUser.Id), cancellationToken);

    return result.ToOkResult(grant => grant);
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
