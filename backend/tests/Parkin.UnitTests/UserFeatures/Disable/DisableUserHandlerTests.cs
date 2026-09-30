using Ardalis.Result;
using NSubstitute;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.StaffUsers;
using Parkin.Api.Features.Users.Disable;
using Shouldly;
using Xunit;
using static Parkin.UnitTests.UserFeatures.StaffUserFixtures;

namespace Parkin.UnitTests.UserFeatures.Disable;

public class DisableUserHandlerTests
{
  private readonly IStaffUserService _staffUsers = Substitute.For<IStaffUserService>();
  private readonly Guid _actorId = Guid.NewGuid();

  private DisableUserHandler CreateSut() => new(_staffUsers, FixedClock());

  [Fact]
  public async Task Handle_UnknownUser_ReturnsNotFound()
  {
    _staffUsers.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((StaffUser?)null);

    var result = await CreateSut().Handle(new DisableUserCommand(Guid.NewGuid(), _actorId), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.NotFound);
    await _staffUsers.DidNotReceiveWithAnyArgs().DisableAsync(default, default!, default);
  }

  [Fact]
  public async Task Handle_LastActiveSystemAdmin_ReturnsConflict()
  {
    var admin = User(StaffRoles.SystemAdmin);
    _staffUsers.FindByIdAsync(admin.Id, Arg.Any<CancellationToken>()).Returns(admin);
    _staffUsers.CountActiveSystemAdminsAsync(Arg.Any<CancellationToken>()).Returns(1);

    var result = await CreateSut().Handle(new DisableUserCommand(admin.Id, _actorId), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Conflict);
    result.Errors.ShouldContain("Cannot disable the last active System Admin.");
    await _staffUsers.DidNotReceiveWithAnyArgs().DisableAsync(default, default!, default);
  }

  [Fact]
  public async Task Handle_AdminWithAnotherActiveAdmin_DisablesWithAuditEntry()
  {
    var admin = User(StaffRoles.SystemAdmin);
    _staffUsers.FindByIdAsync(admin.Id, Arg.Any<CancellationToken>()).Returns(admin);
    _staffUsers.CountActiveSystemAdminsAsync(Arg.Any<CancellationToken>()).Returns(2);
    _staffUsers.DisableAsync(admin.Id, Arg.Any<AuditLogEntry>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

    var result = await CreateSut().Handle(new DisableUserCommand(admin.Id, _actorId), CancellationToken.None);

    result.IsSuccess.ShouldBeTrue();
    await _staffUsers.Received(1).DisableAsync(admin.Id,
      Arg.Is<AuditLogEntry>(entry =>
        entry.Action == AuditActions.UserDisable &&
        entry.EntityType == AuditEntityTypes.User &&
        entry.EntityId == admin.Id &&
        entry.ActorId == _actorId &&
        entry.ActorType == AuditActorType.Staff &&
        entry.OccurredAt == Now),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Operator_SkipsAdminCount()
  {
    var operatorUser = User(StaffRoles.Operator);
    _staffUsers.FindByIdAsync(operatorUser.Id, Arg.Any<CancellationToken>()).Returns(operatorUser);
    _staffUsers.DisableAsync(operatorUser.Id, Arg.Any<AuditLogEntry>(), Arg.Any<CancellationToken>())
      .Returns(Result.Success());

    var result = await CreateSut().Handle(new DisableUserCommand(operatorUser.Id, _actorId), CancellationToken.None);

    result.IsSuccess.ShouldBeTrue();
    await _staffUsers.DidNotReceive().CountActiveSystemAdminsAsync(Arg.Any<CancellationToken>());
  }
}
