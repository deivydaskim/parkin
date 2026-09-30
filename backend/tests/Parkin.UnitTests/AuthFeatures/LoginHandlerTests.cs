using NSubstitute;
using Parkin.Api.Domain.StaffUsers;
using Parkin.Api.Features.Auth.Login;
using Shouldly;
using Xunit;
using static Parkin.UnitTests.UserFeatures.StaffUserFixtures;

namespace Parkin.UnitTests.AuthFeatures;

public class LoginHandlerTests
{
  private readonly IStaffUserService _staffUsers = Substitute.For<IStaffUserService>();
  private readonly IStaffAuthService _staffAuth = Substitute.For<IStaffAuthService>();

  private LoginHandler CreateSut() => new(_staffUsers, _staffAuth);

  [Fact]
  public async Task Handle_UnknownEmail_ReturnsInvalidCredentials()
  {
    _staffUsers.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((StaffUser?)null);

    var result = await CreateSut().Handle(new LoginCommand("nobody@parkin.local", "x"), CancellationToken.None);

    result.Outcome.ShouldBe(StaffSignInResult.InvalidCredentials);
    result.User.ShouldBeNull();
  }

  [Fact]
  public async Task Handle_DisabledUser_ReturnsInvalidCredentials_WithoutSigningIn()
  {
    var disabled = User(StaffRoles.Operator, UserStatus.Disabled);
    _staffUsers.FindByEmailAsync(disabled.Email, Arg.Any<CancellationToken>()).Returns(disabled);

    var result = await CreateSut().Handle(new LoginCommand(disabled.Email, "x"), CancellationToken.None);

    result.Outcome.ShouldBe(StaffSignInResult.InvalidCredentials);
    await _staffAuth.DidNotReceiveWithAnyArgs().PasswordSignInAsync(default, default!, default);
  }

  [Theory]
  [InlineData(StaffSignInResult.LockedOut)]
  [InlineData(StaffSignInResult.InvalidCredentials)]
  public async Task Handle_SignInFailure_PropagatesOutcome(StaffSignInResult outcome)
  {
    var user = User(StaffRoles.Operator);
    _staffUsers.FindByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
    _staffAuth.PasswordSignInAsync(user.Id, "x", Arg.Any<CancellationToken>()).Returns(outcome);

    var result = await CreateSut().Handle(new LoginCommand(user.Email, "x"), CancellationToken.None);

    result.Outcome.ShouldBe(outcome);
    result.User.ShouldBeNull();
  }

  [Fact]
  public async Task Handle_Success_ReturnsCurrentUser()
  {
    var user = User(StaffRoles.SystemAdmin);
    _staffUsers.FindByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
    _staffAuth.PasswordSignInAsync(user.Id, "secret", Arg.Any<CancellationToken>())
      .Returns(StaffSignInResult.Succeeded);

    var result = await CreateSut().Handle(new LoginCommand(user.Email, "secret"), CancellationToken.None);

    result.Outcome.ShouldBe(StaffSignInResult.Succeeded);
    result.User.ShouldNotBeNull();
    result.User.Id.ShouldBe(user.Id);
    result.User.Roles.ShouldBe([StaffRoles.SystemAdmin]);
  }
}
