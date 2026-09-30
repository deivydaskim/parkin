using Ardalis.Result;
using NSubstitute;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.StaffUsers;
using Parkin.Api.Features.Users.ChangeRole;
using Shouldly;
using Xunit;
using static Parkin.UnitTests.UserFeatures.StaffUserFixtures;

namespace Parkin.UnitTests.UserFeatures.ChangeRole;

public class ChangeUserRoleHandlerTests
{
  private readonly IStaffUserService _staffUsers = Substitute.For<IStaffUserService>();
  private readonly Guid _actorId = Guid.NewGuid();

  private ChangeUserRoleHandler CreateSut() => new(_staffUsers, FixedClock());

  [Fact]
  public async Task Handle_UnknownUser_ReturnsNotFound()
  {
    _staffUsers.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((StaffUser?)null);

    var result = await CreateSut().Handle(
      new ChangeUserRoleCommand(Guid.NewGuid(), StaffRoles.Operator, _actorId), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_DemotingLastActiveSystemAdmin_ReturnsConflict()
  {
    var admin = User(StaffRoles.SystemAdmin);
    _staffUsers.FindByIdAsync(admin.Id, Arg.Any<CancellationToken>()).Returns(admin);
    _staffUsers.CountActiveSystemAdminsAsync(Arg.Any<CancellationToken>()).Returns(1);

    var result = await CreateSut().Handle(
      new ChangeUserRoleCommand(admin.Id, StaffRoles.Operator, _actorId), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Conflict);
    await _staffUsers.DidNotReceiveWithAnyArgs().ChangeRoleAsync(default, default!, default!, default);
  }

  [Fact]
  public async Task Handle_KeepingLastAdminAsSystemAdmin_IsAllowed()
  {
    var admin = User(StaffRoles.SystemAdmin);
    _staffUsers.FindByIdAsync(admin.Id, Arg.Any<CancellationToken>()).Returns(admin);
    _staffUsers.ChangeRoleAsync(admin.Id, StaffRoles.SystemAdmin, Arg.Any<AuditLogEntry>(), Arg.Any<CancellationToken>())
      .Returns(admin);

    var result = await CreateSut().Handle(
      new ChangeUserRoleCommand(admin.Id, StaffRoles.SystemAdmin, _actorId), CancellationToken.None);

    result.IsSuccess.ShouldBeTrue();
    await _staffUsers.DidNotReceive().CountActiveSystemAdminsAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_PromotingOperator_ChangesRoleWithAuditMetadata()
  {
    var operatorUser = User(StaffRoles.Operator);
    var promoted = operatorUser with { Roles = [StaffRoles.SystemAdmin] };
    _staffUsers.FindByIdAsync(operatorUser.Id, Arg.Any<CancellationToken>()).Returns(operatorUser);
    _staffUsers.ChangeRoleAsync(operatorUser.Id, StaffRoles.SystemAdmin, Arg.Any<AuditLogEntry>(),
        Arg.Any<CancellationToken>())
      .Returns(promoted);

    var result = await CreateSut().Handle(
      new ChangeUserRoleCommand(operatorUser.Id, StaffRoles.SystemAdmin, _actorId), CancellationToken.None);

    result.IsSuccess.ShouldBeTrue();
    result.Value.Role.ShouldBe(StaffRoles.SystemAdmin);
    result.Value.Status.ShouldBe(nameof(UserStatus.Active));
    await _staffUsers.Received(1).ChangeRoleAsync(operatorUser.Id, StaffRoles.SystemAdmin,
      Arg.Is<AuditLogEntry>(entry =>
        entry.Action == AuditActions.UserChangeRole &&
        entry.EntityId == operatorUser.Id &&
        entry.ActorId == _actorId &&
        entry.OccurredAt == Now &&
        entry.MetadataJson!.Contains("\"fromRole\":\"Operator\"") &&
        entry.MetadataJson.Contains("\"toRole\":\"SystemAdmin\"")),
      Arg.Any<CancellationToken>());
  }
}
