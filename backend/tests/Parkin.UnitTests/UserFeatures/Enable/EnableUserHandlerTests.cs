using Ardalis.Result;
using NSubstitute;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.StaffUsers;
using Parkin.Api.Features.Users.Enable;
using Shouldly;
using Xunit;
using static Parkin.UnitTests.UserFeatures.StaffUserFixtures;

namespace Parkin.UnitTests.UserFeatures.Enable;

public class EnableUserHandlerTests
{
  private readonly IStaffUserService _staffUsers = Substitute.For<IStaffUserService>();

  private EnableUserHandler CreateSut() => new(_staffUsers, FixedClock());

  [Fact]
  public async Task Handle_UnknownUser_ReturnsNotFound()
  {
    _staffUsers.EnableAsync(Arg.Any<Guid>(), Arg.Any<AuditLogEntry>(), Arg.Any<CancellationToken>())
      .Returns(Result.NotFound());

    var result = await CreateSut().Handle(new EnableUserCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_PassesAuditEntryForTheTargetUser()
  {
    var userId = Guid.NewGuid();
    var actorId = Guid.NewGuid();
    _staffUsers.EnableAsync(userId, Arg.Any<AuditLogEntry>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

    var result = await CreateSut().Handle(new EnableUserCommand(userId, actorId), CancellationToken.None);

    result.IsSuccess.ShouldBeTrue();
    await _staffUsers.Received(1).EnableAsync(userId,
      Arg.Is<AuditLogEntry>(entry =>
        entry.Action == AuditActions.UserEnable && entry.EntityId == userId && entry.ActorId == actorId &&
        entry.OccurredAt == Now),
      Arg.Any<CancellationToken>());
  }
}
