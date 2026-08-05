using Parkin.Api.Domain.Services;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.Domain;

public class OccupancyCalculatorTests
{
  private readonly OccupancyCalculator _sut = new();

  // ---------------------------------------------------------------------------
  // general free = max(0, capacity - active GENERAL sessions)
  // ---------------------------------------------------------------------------

  [Theory]
  [InlineData(10, 0, 10)]
  [InlineData(10, 4, 6)]
  [InlineData(10, 9, 1)]
  [InlineData(1, 0, 1)]
  public void Calculate_BelowCapacity_ComputesExactFreeCount(int capacity, int used, int expectedFree)
  {
    var context = OccupancyContext.Create(capacity, used);

    var result = _sut.Calculate(context);

    result.GeneralFree.ShouldBe(expectedFree);
    result.GeneralUsed.ShouldBe(used);
    result.GeneralCapacity.ShouldBe(capacity);
    result.IsGeneralPoolFull.ShouldBeFalse();
    result.IsOverCapacity.ShouldBeFalse();
  }

  [Fact]
  public void Calculate_OneUnderCapacity_IsNotFull()
  {
    var context = OccupancyContext.Create(generalCapacity: 10, activeGeneralSessionCount: 9);

    var result = _sut.Calculate(context);

    result.GeneralFree.ShouldBe(1);
    result.IsGeneralPoolFull.ShouldBeFalse();
  }

  [Fact]
  public void Calculate_ExactlyAtCapacity_IsFullWithZeroFree_NotOverCapacity()
  {
    var context = OccupancyContext.Create(generalCapacity: 10, activeGeneralSessionCount: 10);

    var result = _sut.Calculate(context);

    result.GeneralFree.ShouldBe(0);
    result.IsGeneralPoolFull.ShouldBeTrue();
    result.IsOverCapacity.ShouldBeFalse();
  }

  // ---------------------------------------------------------------------------
  // Floor-at-zero: once used exceeds capacity (an ALLOW_OVERFLOW admission), free must
  // never go negative, and the over-capacity flag distinguishes this from exactly-full.
  // ---------------------------------------------------------------------------

  [Theory]
  [InlineData(10, 11)]
  [InlineData(10, 15)]
  [InlineData(0, 1)]
  [InlineData(1, 100)]
  public void Calculate_OverCapacity_FreeFlooredAtZero_OverCapacityFlagSet(int capacity, int used)
  {
    var context = OccupancyContext.Create(capacity, used);

    var result = _sut.Calculate(context);

    result.GeneralFree.ShouldBe(0);
    result.IsGeneralPoolFull.ShouldBeTrue();
    result.IsOverCapacity.ShouldBeTrue();
  }

  // ---------------------------------------------------------------------------
  // Capacity = 0 edge case: a lot with no active GENERAL spaces at all is trivially full
  // (there is nothing to be free), but not over-capacity until something is actually admitted.
  // ---------------------------------------------------------------------------

  [Fact]
  public void Calculate_ZeroCapacityAndZeroUsed_IsFullButNotOverCapacity()
  {
    var context = OccupancyContext.Create(generalCapacity: 0, activeGeneralSessionCount: 0);

    var result = _sut.Calculate(context);

    result.GeneralFree.ShouldBe(0);
    result.IsGeneralPoolFull.ShouldBeTrue();
    result.IsOverCapacity.ShouldBeFalse();
  }

  [Fact]
  public void Calculate_ZeroCapacityWithAnyUsed_IsOverCapacity()
  {
    var context = OccupancyContext.Create(generalCapacity: 0, activeGeneralSessionCount: 1);

    var result = _sut.Calculate(context);

    result.GeneralFree.ShouldBe(0);
    result.IsOverCapacity.ShouldBeTrue();
  }

  // ---------------------------------------------------------------------------
  // Reserved bypass: reserved sessions occupy dedicated spaces, never the shared general
  // pool - varying the reserved count must never move GeneralFree/Used/Full/OverCapacity.
  // The reserved count is only echoed through for reporting (T5.3's occupancy view).
  // ---------------------------------------------------------------------------

  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  [InlineData(50)]
  public void Calculate_ReservedSessionCount_NeverAffectsGeneralMath(int reservedCount)
  {
    var context = OccupancyContext.Create(generalCapacity: 5, activeGeneralSessionCount: 5, activeReservedSessionCount: reservedCount);

    var result = _sut.Calculate(context);

    result.GeneralFree.ShouldBe(0);
    result.GeneralUsed.ShouldBe(5);
    result.IsGeneralPoolFull.ShouldBeTrue();
    result.IsOverCapacity.ShouldBeFalse();
    result.ReservedCount.ShouldBe(reservedCount);
  }

  [Fact]
  public void Calculate_ReservedSessionsPresent_GeneralPoolStillHasFreeSpace()
  {
    var context = OccupancyContext.Create(generalCapacity: 10, activeGeneralSessionCount: 2, activeReservedSessionCount: 20);

    var result = _sut.Calculate(context);

    result.GeneralFree.ShouldBe(8);
    result.ReservedCount.ShouldBe(20);
  }

  [Fact]
  public void Calculate_DefaultsReservedCountToZero_WhenNotSpecified()
  {
    var context = OccupancyContext.Create(generalCapacity: 5, activeGeneralSessionCount: 1);

    var result = _sut.Calculate(context);

    result.ReservedCount.ShouldBe(0);
  }

  [Fact]
  public void Calculate_ThrowsOnNullContext()
  {
    Should.Throw<ArgumentNullException>(() => _sut.Calculate(null!));
  }
}

public class OccupancyContextTests
{
  [Fact]
  public void Create_NegativeCapacity_Throws()
  {
    Should.Throw<ArgumentOutOfRangeException>(() => OccupancyContext.Create(generalCapacity: -1, activeGeneralSessionCount: 0));
  }

  [Fact]
  public void Create_NegativeActiveGeneralSessionCount_Throws()
  {
    Should.Throw<ArgumentOutOfRangeException>(() => OccupancyContext.Create(generalCapacity: 5, activeGeneralSessionCount: -1));
  }

  [Fact]
  public void Create_NegativeActiveReservedSessionCount_Throws()
  {
    Should.Throw<ArgumentOutOfRangeException>(() => OccupancyContext.Create(generalCapacity: 5, activeGeneralSessionCount: 0, activeReservedSessionCount: -1));
  }
}
