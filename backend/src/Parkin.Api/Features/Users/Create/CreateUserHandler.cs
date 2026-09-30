using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.StaffUsers;

namespace Parkin.Api.Features.Users.Create;

public record CreateUserCommand(string Email, string Password, string DisplayName, string Role, Guid? ActorId)
  : ICommand<Result<UserResponse>>;

public class CreateUserHandler(IStaffUserService staffUsers, TimeProvider timeProvider)
  : ICommandHandler<CreateUserCommand, Result<UserResponse>>
{
  public async ValueTask<Result<UserResponse>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
  {
    var newUser = new NewStaffUser(Guid.CreateVersion7(), command.Email, command.DisplayName, command.Password,
      command.Role);
    var audit = UserAudit.Entry(AuditActions.UserCreate, command.ActorId, newUser.Id, timeProvider,
      new { role = command.Role });

    var result = await staffUsers.CreateAsync(newUser, audit, cancellationToken);
    return result.Map(UserResponse.From);
  }
}
