using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.StaffUsers;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Users.ChangeRole;

public sealed class ChangeUserRoleRequest
{
  public const string Route = "/users/{UserId}/role";

  public Guid UserId { get; init; }
  public string Role { get; init; } = string.Empty;
}

public class ChangeUserRoleEndpoint(IMediator mediator, ICurrentUser currentUser) :
  Endpoint<ChangeUserRoleRequest, Results<Ok<UserResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Patch(ChangeUserRoleRequest.Route);
    Roles(AccessPolicies.AdminOnly);

    Summary(s =>
    {
      s.Summary = "Change a staff account's role";
      s.Description = "Replaces the staff account's single role. Existing sessions gain/lose access after the next security-stamp revalidation.";
      s.Responses[200] = "Role changed successfully";
      s.Responses[404] = "User with specified ID not found";
      s.Responses[409] = "Cannot demote the last active System Admin";
    });

    Tags("Users");
  }

  public override async Task<Results<Ok<UserResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(ChangeUserRoleRequest request, CancellationToken cancellationToken)
  {
    var command = new ChangeUserRoleCommand(request.UserId, request.Role, currentUser.Id);
    var result = await mediator.Send(command, cancellationToken);
    return result.ToOkResult(user => user);
  }
}

public sealed class ChangeUserRoleValidator : Validator<ChangeUserRoleRequest>
{
  public ChangeUserRoleValidator()
  {
    RuleFor(x => x.UserId)
      .NotEmpty()
      .WithMessage("User ID is required");

    RuleFor(x => x.Role)
      .Must(v => StaffRoles.All.Contains(v))
      .WithMessage($"Role must be one of: {string.Join(", ", StaffRoles.All)}");
  }
}
