using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.StaffUsers;

namespace Parkin.Api.Features.Users.Disable;

public record DisableUserCommand(Guid UserId, Guid? ActorId) : ICommand<Result>;

public class DisableUserHandler(IStaffUserService staffUsers, TimeProvider timeProvider)
  : ICommandHandler<DisableUserCommand, Result>
{
  public async ValueTask<Result> Handle(DisableUserCommand command, CancellationToken cancellationToken)
  {
    var user = await staffUsers.FindByIdAsync(command.UserId, cancellationToken);
    if (user is null) return Result.NotFound();

    if (await staffUsers.IsLastActiveSystemAdminAsync(user, cancellationToken))
    {
      return Result.Conflict("Cannot disable the last active System Admin.");
    }

    var audit = UserAudit.Entry(AuditActions.UserDisable, command.ActorId, user.Id, timeProvider);
    return await staffUsers.DisableAsync(user.Id, audit, cancellationToken);
  }
}
