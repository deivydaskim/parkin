using Parkin.Api.UserFeatures;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.UserFeatures;

public class UserGuardrailsTests
{
  [Theory]
  [InlineData(1, true, true)]   // sole active admin, target is that admin -> blocks
  [InlineData(2, true, false)]  // another active admin exists -> allows
  [InlineData(1, false, false)] // target isn't the active admin being counted -> never blocks
  [InlineData(0, true, true)]   // target counted as active admin but nobody else -> blocks
  public void WouldRemoveLastActiveSystemAdmin_TruthTable(
    int activeSystemAdminCount, bool targetIsActiveSystemAdmin, bool expected)
  {
    UserGuardrails.WouldRemoveLastActiveSystemAdmin(activeSystemAdminCount, targetIsActiveSystemAdmin)
      .ShouldBe(expected);
  }
}
