using NSubstitute;
using Parkin.Api.Domain.StaffUsers;

namespace Parkin.UnitTests.UserFeatures;

internal static class StaffUserFixtures
{
  public static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

  public static TimeProvider FixedClock()
  {
    var clock = Substitute.For<TimeProvider>();
    clock.GetUtcNow().Returns(Now);
    return clock;
  }

  public static StaffUser User(string role, UserStatus status = UserStatus.Active)
    => new(Guid.NewGuid(), $"{Guid.NewGuid():N}@parkin.local", "Staff Member", [role], status);
}
