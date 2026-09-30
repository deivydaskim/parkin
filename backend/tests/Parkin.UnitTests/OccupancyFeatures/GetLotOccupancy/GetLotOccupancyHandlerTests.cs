using Ardalis.Result;
using NSubstitute;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Features.Occupancy;
using Parkin.Api.Features.Occupancy.GetLotOccupancy;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.OccupancyFeatures.GetLotOccupancy;

public class GetLotOccupancyHandlerTests
{
  private static readonly DateTimeOffset Now = new(2026, 8, 25, 9, 0, 0, TimeSpan.Zero);

  private readonly ILotOccupancyQueryService _query = Substitute.For<ILotOccupancyQueryService>();
  private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
  private readonly ParkingLotId _lotId = ParkingLotId.From(Guid.NewGuid());

  public GetLotOccupancyHandlerTests() => _timeProvider.GetUtcNow().Returns(Now);

  private GetLotOccupancyHandler CreateSut() => new(_query, _timeProvider);

  private void GivenLot(int generalCapacity, int generalSessions, int reservedSessions = 0, int reservedSpaces = 0) =>
    _query.FindAsync(_lotId, Arg.Any<CancellationToken>())
      .Returns(new LotOccupancyInputs(_lotId, "Test Lot", generalCapacity, reservedSpaces, generalSessions,
        reservedSessions));

  private async Task<Result<LotOccupancyResponse>> HandleAsync() =>
    await CreateSut().Handle(new GetLotOccupancyQuery(_lotId), CancellationToken.None);

  [Fact]
  public async Task Handle_UnknownLot_ReturnsNotFound()
  {
    _query.FindAsync(_lotId, Arg.Any<CancellationToken>()).Returns((LotOccupancyInputs?)null);

    var result = await HandleAsync();

    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_EmptyLot_ReportsFullCapacityFree()
  {
    GivenLot(generalCapacity: 3, generalSessions: 0);

    var result = await HandleAsync();

    result.Status.ShouldBe(ResultStatus.Ok);
    result.Value.LotId.ShouldBe(_lotId.Value);
    result.Value.LotName.ShouldBe("Test Lot");
    result.Value.GeneralCapacity.ShouldBe(3);
    result.Value.GeneralUsed.ShouldBe(0);
    result.Value.GeneralFree.ShouldBe(3);
    result.Value.IsGeneralPoolFull.ShouldBeFalse();
    result.Value.IsOverCapacity.ShouldBeFalse();
  }

  [Fact]
  public async Task Handle_PartiallyOccupied_DerivesUsedAndFree()
  {
    GivenLot(generalCapacity: 5, generalSessions: 2);

    var result = await HandleAsync();

    result.Value.GeneralCapacity.ShouldBe(5);
    result.Value.GeneralUsed.ShouldBe(2);
    result.Value.GeneralFree.ShouldBe(3);
  }

  [Fact]
  public async Task Handle_ExactlyFull_FlagsFullButNotOverCapacity()
  {
    GivenLot(generalCapacity: 2, generalSessions: 2);

    var result = await HandleAsync();

    result.Value.GeneralFree.ShouldBe(0);
    result.Value.IsGeneralPoolFull.ShouldBeTrue();
    result.Value.IsOverCapacity.ShouldBeFalse();
  }

  [Fact]
  public async Task Handle_MoreSessionsThanCapacity_FloorsFreeAtZeroAndFlagsOverCapacity()
  {
    GivenLot(generalCapacity: 2, generalSessions: 5);

    var result = await HandleAsync();

    result.Value.GeneralUsed.ShouldBe(5);
    result.Value.GeneralFree.ShouldBe(0);
    result.Value.IsOverCapacity.ShouldBeTrue();
  }

  [Fact]
  public async Task Handle_ReservedSessions_DoNotConsumeTheGeneralPool()
  {
    GivenLot(generalCapacity: 2, generalSessions: 1, reservedSessions: 2, reservedSpaces: 2);

    var result = await HandleAsync();

    result.Value.ReservedSpaceCount.ShouldBe(2);
    result.Value.ReservedOccupied.ShouldBe(2);
    result.Value.GeneralUsed.ShouldBe(1);
    result.Value.GeneralFree.ShouldBe(1);
    result.Value.IsGeneralPoolFull.ShouldBeFalse();
  }

  [Fact]
  public async Task Handle_StampsAsOfFromTheTimeProvider()
  {
    GivenLot(generalCapacity: 1, generalSessions: 0);

    var result = await HandleAsync();

    result.Value.AsOf.ShouldBe(Now);
  }
}
