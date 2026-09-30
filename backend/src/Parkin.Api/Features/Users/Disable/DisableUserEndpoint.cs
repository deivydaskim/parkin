using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Users.Disable;

public sealed class DisableUserRequest
{
  public const string Route = "/users/{UserId}/disable";

  public Guid UserId { get; init; }
}

public class DisableUserEndpoint(IMediator mediator, ICurrentUser currentUser) :
  Endpoint<DisableUserRequest, Results<NoContent, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(DisableUserRequest.Route);
    Roles(AccessPolicies.AdminOnly);

    Summary(s =>
    {
      s.Summary = "Disable a staff account";
      s.Description = "Disables a staff account and rotates its security stamp so any live session is rejected on the next revalidation.";
      s.Responses[204] = "User disabled successfully";
      s.Responses[404] = "User with specified ID not found";
      s.Responses[409] = "Cannot disable the last active System Admin";
    });

    Tags("Users");
  }

  public override async Task<Results<NoContent, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(DisableUserRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new DisableUserCommand(request.UserId, currentUser.Id), cancellationToken);
    return result.ToNoContentResult();
  }
}

public sealed class DisableUserValidator : Validator<DisableUserRequest>
{
  public DisableUserValidator()
  {
    RuleFor(x => x.UserId)
      .NotEmpty()
      .WithMessage("User ID is required");
  }
}
