using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.StaffUsers;

namespace Parkin.Api.Features.Users.Enable;

public record EnableUserCommand(Guid UserId, Guid? ActorId) : ICommand<Result>;

public class EnableUserHandler(IStaffUserService staffUsers, TimeProvider timeProvider)
  : ICommandHandler<EnableUserCommand, Result>
{
  public async ValueTask<Result> Handle(EnableUserCommand command, CancellationToken cancellationToken)
  {
    var audit = UserAudit.Entry(AuditActions.UserEnable, command.ActorId, command.UserId, timeProvider);
    return await staffUsers.EnableAsync(command.UserId, audit, cancellationToken);
  }
}
