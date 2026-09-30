using Ardalis.Result;
using NSubstitute;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.StaffUsers;
using Parkin.Api.Features.Users.Create;
using Shouldly;
using Xunit;
using static Parkin.UnitTests.UserFeatures.StaffUserFixtures;

namespace Parkin.UnitTests.UserFeatures.Create;

public class CreateUserHandlerTests
{
  private readonly IStaffUserService _staffUsers = Substitute.For<IStaffUserService>();
  private readonly Guid _actorId = Guid.NewGuid();

  private CreateUserHandler CreateSut() => new(_staffUsers, FixedClock());

  private static CreateUserCommand Command(Guid actorId)
    => new("new.operator@parkin.local", "Secret!2345", "New Operator", StaffRoles.Operator, actorId);

  [Fact]
  public async Task Handle_Success_AuditsTheNewUserIdAndMapsResponse()
  {
    _staffUsers.CreateAsync(Arg.Any<NewStaffUser>(), Arg.Any<AuditLogEntry>(), Arg.Any<CancellationToken>())
      .Returns(call =>
      {
        var user = call.Arg<NewStaffUser>();
        return new StaffUser(user.Id, user.Email, user.DisplayName, [user.Role], UserStatus.Active);
      });

    var result = await CreateSut().Handle(Command(_actorId), CancellationToken.None);

    result.IsSuccess.ShouldBeTrue();
    result.Value.Email.ShouldBe("new.operator@parkin.local");
    result.Value.Role.ShouldBe(StaffRoles.Operator);
    await _staffUsers.Received(1).CreateAsync(
      Arg.Is<NewStaffUser>(user => user.Password == "Secret!2345" && user.Id != Guid.Empty),
      Arg.Is<AuditLogEntry>(entry =>
        entry.Action == AuditActions.UserCreate &&
        entry.EntityId == result.Value.Id &&
        entry.ActorId == _actorId &&
        entry.OccurredAt == Now),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_IdentityRejectsPassword_ReturnsInvalid()
  {
    _staffUsers.CreateAsync(Arg.Any<NewStaffUser>(), Arg.Any<AuditLogEntry>(), Arg.Any<CancellationToken>())
      .Returns(Result<StaffUser>.Invalid(new ValidationError("Password", "Passwords must have at least one digit.")));

    var result = await CreateSut().Handle(Command(_actorId), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Invalid);
    result.ValidationErrors.ShouldContain(error => error.Identifier == "Password");
  }
}
