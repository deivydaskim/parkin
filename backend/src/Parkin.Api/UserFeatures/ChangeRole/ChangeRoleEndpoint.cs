using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Infrastructure.Identity;

namespace Parkin.Api.UserFeatures.ChangeRole;

public sealed class ChangeRoleRequest
{
  public const string Route = "/users/{UserId}/role";

  public Guid UserId { get; init; }
  public string Role { get; init; } = string.Empty;
}

public class ChangeRoleEndpoint(UserManager<ApplicationUser> userManager, IRepository<AuditLogEntry> auditRepository) :
  Endpoint<ChangeRoleRequest, Results<Ok<UserRecord>, NotFound, ProblemHttpResult>>
{
  public override void Configure()
  {
    Patch(ChangeRoleRequest.Route);
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

  public override async Task<Results<Ok<UserRecord>, NotFound, ProblemHttpResult>>
    ExecuteAsync(ChangeRoleRequest request, CancellationToken cancellationToken)
  {
    var user = await userManager.FindByIdAsync(request.UserId.ToString());
    if (user is null) return TypedResults.NotFound();

    var currentRoles = await userManager.GetRolesAsync(user);
    var fromRole = currentRoles.FirstOrDefault() ?? string.Empty;

    if (fromRole == Parkin.Api.Infrastructure.Identity.Roles.SystemAdmin
        && request.Role != Parkin.Api.Infrastructure.Identity.Roles.SystemAdmin
        && await UserGuardrails.IsLastActiveSystemAdminAsync(userManager, user))
    {
      return TypedResults.Problem(
        title: "Change role failed",
        detail: "Cannot demote the last active System Admin.",
        statusCode: StatusCodes.Status409Conflict);
    }

    if (currentRoles.Count > 0)
    {
      await userManager.RemoveFromRolesAsync(user, currentRoles);
    }
    await userManager.AddToRoleAsync(user, request.Role);

    var entry = AuditLogEntry.Create(
      AuditActorType.Staff,
      HttpContext.User.ActorId(userManager),
      AuditActions.UserChangeRole,
      AuditEntityTypes.User,
      user.Id,
      HttpContext.Connection.RemoteIpAddress,
      new { fromRole, toRole = request.Role });
    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);

    var response = new UserRecord(user.Id, user.Email!, user.DisplayName, request.Role, user.Status.ToString());
    return TypedResults.Ok(response);
  }
}

public sealed class ChangeRoleValidator : Validator<ChangeRoleRequest>
{
  public ChangeRoleValidator()
  {
    RuleFor(x => x.UserId)
      .NotEmpty()
      .WithMessage("User ID is required");

    RuleFor(x => x.Role)
      .Must(v => Roles.All.Contains(v))
      .WithMessage($"Role must be one of: {string.Join(", ", Roles.All)}");
  }
}
