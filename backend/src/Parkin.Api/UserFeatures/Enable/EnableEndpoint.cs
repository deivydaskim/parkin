using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Infrastructure.Identity;

namespace Parkin.Api.UserFeatures.Enable;

public sealed class EnableUserRequest
{
  public const string Route = "/users/{UserId}/enable";

  public Guid UserId { get; init; }
}

public class EnableEndpoint(UserManager<ApplicationUser> userManager, IRepository<AuditLogEntry> auditRepository) :
  Endpoint<EnableUserRequest, Results<NoContent, NotFound, ProblemHttpResult>>
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

  public override async Task<Results<NoContent, NotFound, ProblemHttpResult>>
    ExecuteAsync(EnableUserRequest request, CancellationToken cancellationToken)
  {
    var user = await userManager.FindByIdAsync(request.UserId.ToString());
    if (user is null) return TypedResults.NotFound();

    user.Status = UserStatus.Active;
    await userManager.UpdateAsync(user);

    var entry = AuditLogEntry.Create(
      AuditActorType.Staff,
      HttpContext.User.ActorId(userManager),
      "user.enable",
      "User",
      user.Id,
      HttpContext.Connection.RemoteIpAddress);
    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);

    return TypedResults.NoContent();
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
