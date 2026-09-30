using Parkin.Api.Domain.StaffUsers;

namespace Parkin.Api.Features.Auth.Login;

public record LoginCommand(string Email, string Password) : ICommand<LoginResult>;

public sealed record LoginResult(StaffSignInResult Outcome, CurrentUserResponse? User)
{
  public static LoginResult Failed(StaffSignInResult outcome) => new(outcome, null);
}

public class LoginHandler(IStaffUserService staffUsers, IStaffAuthService staffAuth)
  : ICommandHandler<LoginCommand, LoginResult>
{
  public async ValueTask<LoginResult> Handle(LoginCommand command, CancellationToken cancellationToken)
  {
    var user = await staffUsers.FindByEmailAsync(command.Email, cancellationToken);
    if (user is null || user.Status == UserStatus.Disabled)
    {
      return LoginResult.Failed(StaffSignInResult.InvalidCredentials);
    }

    var outcome = await staffAuth.PasswordSignInAsync(user.Id, command.Password, cancellationToken);
    return outcome == StaffSignInResult.Succeeded
      ? new LoginResult(outcome, CurrentUserResponse.From(user))
      : LoginResult.Failed(outcome);
  }
}
