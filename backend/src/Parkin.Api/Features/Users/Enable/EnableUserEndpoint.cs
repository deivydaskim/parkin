using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Users.Enable;

public sealed class EnableUserRequest
{
  public const string Route = "/users/{UserId}/enable";

  public Guid UserId { get; init; }
}

public class EnableUserEndpoint(IMediator mediator, ICurrentUser currentUser) :
  Endpoint<EnableUserRequest, Results<NoContent, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(EnableUserRequest.Route);
    Roles(AccessPolicies.AdminOnly);

    Summary(s =>
    {
      s.Summary = "Re-enable a disabled staff account";
      s.Responses[204] = "User enabled successfully";
      s.Responses[404] = "User with specified ID not found";
    });

    Tags("Users");
  }

  public override async Task<Results<NoContent, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(EnableUserRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new EnableUserCommand(request.UserId, currentUser.Id), cancellationToken);
    return result.ToNoContentResult();
  }
}

public sealed class EnableUserValidator : Validator<EnableUserRequest>
{
  public EnableUserValidator()
  {
    RuleFor(x => x.UserId)
      .NotEmpty()
      .WithMessage("User ID is required");
  }
}
