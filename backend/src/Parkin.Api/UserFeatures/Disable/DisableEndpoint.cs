using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Infrastructure.Identity;

namespace Parkin.Api.UserFeatures.Disable;

public sealed class DisableUserRequest
{
  public const string Route = "/users/{UserId}/disable";

  public Guid UserId { get; init; }
}

public class DisableEndpoint(UserManager<ApplicationUser> userManager, IRepository<AuditLogEntry> auditRepository) :
  Endpoint<DisableUserRequest, Results<NoContent, NotFound, ProblemHttpResult>>
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

  public override async Task<Results<NoContent, NotFound, ProblemHttpResult>>
    ExecuteAsync(DisableUserRequest request, CancellationToken cancellationToken)
  {
    var user = await userManager.FindByIdAsync(request.UserId.ToString());
    if (user is null) return TypedResults.NotFound();

    if (await UserGuardrails.IsLastActiveSystemAdminAsync(userManager, user))
    {
      return TypedResults.Problem(
        title: "Disable failed",
        detail: "Cannot disable the last active System Admin.",
        statusCode: StatusCodes.Status409Conflict);
    }

    user.Status = UserStatus.Disabled;
    await userManager.UpdateAsync(user);
    await userManager.UpdateSecurityStampAsync(user);

    var entry = AuditLogEntry.Create(
      AuditActorType.Staff,
      HttpContext.User.ActorId(userManager),
      AuditActions.UserDisable,
      AuditEntityTypes.User,
      user.Id);
    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);

    return TypedResults.NoContent();
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
