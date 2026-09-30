using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.StaffUsers;

namespace Parkin.Api.Features.Users.ChangeRole;

public record ChangeUserRoleCommand(Guid UserId, string Role, Guid? ActorId) : ICommand<Result<UserResponse>>;

public class ChangeUserRoleHandler(IStaffUserService staffUsers, TimeProvider timeProvider)
  : ICommandHandler<ChangeUserRoleCommand, Result<UserResponse>>
{
  public async ValueTask<Result<UserResponse>> Handle(ChangeUserRoleCommand command,
    CancellationToken cancellationToken)
  {
    var user = await staffUsers.FindByIdAsync(command.UserId, cancellationToken);
    if (user is null) return Result.NotFound();

    if (command.Role != StaffRoles.SystemAdmin && await staffUsers.IsLastActiveSystemAdminAsync(user, cancellationToken))
    {
      return Result.Conflict("Cannot demote the last active System Admin.");
    }

    var audit = UserAudit.Entry(AuditActions.UserChangeRole, command.ActorId, user.Id, timeProvider,
      new { fromRole = user.Role, toRole = command.Role });

    var result = await staffUsers.ChangeRoleAsync(user.Id, command.Role, audit, cancellationToken);
    return result.Map(UserResponse.From);
  }
}
