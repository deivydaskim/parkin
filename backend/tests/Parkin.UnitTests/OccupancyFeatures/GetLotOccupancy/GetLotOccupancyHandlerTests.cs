using Ardalis.Result;
using Ardalis.SharedKernel;
using NSubstitute;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;
using Parkin.Api.Domain.ParkingSessionAggregate;
using Parkin.Api.Domain.ParkingSessionAggregate.Specifications;
using Parkin.Api.Domain.Services;
using Parkin.Api.OccupancyFeatures.GetLotOccupancy;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.OccupancyFeatures.GetLotOccupancy;

// The arithmetic itself is covered exhaustively by OccupancyCalculatorTests; these cover the
// projection - that the handler feeds the calculator the right inputs and maps its output faithfully.
public class GetLotOccupancyHandlerTests
{
  private readonly IReadRepository<ParkingLot> _lotRepository = Substitute.For<IReadRepository<ParkingLot>>();
  private readonly IReadRepository<ParkingSession> _sessionRepository = Substitute.For<IReadRepository<ParkingSession>>();

  private GetLotOccupancyHandler CreateSut() =>
    new(_lotRepository, _sessionRepository, new OccupancyCalculator());

  private void GivenLot(ParkingLot? lot) =>
    _lotRepository.FirstOrDefaultAsync(Arg.Any<ParkingLotByIdSpec>(), Arg.Any<CancellationToken>())
      .Returns(lot);

  private void GivenSessionCounts(int general, int reserved)
  {
    _sessionRepository.CountAsync(
        Arg.Is<ActiveSessionCountByLotPoolSpec>(spec => spec.Pool == SessionPool.General),
        Arg.Any<CancellationToken>())
      .Returns(general);
    _sessionRepository.CountAsync(
        Arg.Is<ActiveSessionCountByLotPoolSpec>(spec => spec.Pool == SessionPool.Reserved),
        Arg.Any<CancellationToken>())
      .Returns(reserved);
  }

  private async Task<Result<Parkin.Api.OccupancyFeatures.LotOccupancyDto>> HandleAsync(ParkingLot lot) =>
    await CreateSut().Handle(new GetLotOccupancyQuery(lot.Id), CancellationToken.None);

  private static ParkingLot LotWithSpaces(int general, int reserved)
  {
    var lot = ParkingLot.Create("Test Lot", "Europe/Vilnius");
    for (var i = 0; i < general; i++) lot.AddSpace($"G{i}", SpaceType.General, actorId: null);
    for (var i = 0; i < reserved; i++) lot.AddSpace($"R{i}", SpaceType.Reserved, actorId: null);
    return lot;
  }

  [Fact]
  public async Task Handle_UnknownLot_ReturnsNotFound()
  {
    GivenLot(null);

    var result = await CreateSut().Handle(
      new GetLotOccupancyQuery(ParkingLotId.From(Guid.NewGuid())), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_EmptyLot_ReportsFullCapacityFree()
  {
    var lot = LotWithSpaces(general: 3, reserved: 0);
    GivenLot(lot);
    GivenSessionCounts(general: 0, reserved: 0);

    var result = await HandleAsync(lot);

    result.Status.ShouldBe(ResultStatus.Ok);
    result.Value.LotId.ShouldBe(lot.Id);
    result.Value.GeneralCapacity.ShouldBe(3);
    result.Value.GeneralUsed.ShouldBe(0);
    result.Value.GeneralFree.ShouldBe(3);
    result.Value.IsGeneralPoolFull.ShouldBeFalse();
    result.Value.IsOverCapacity.ShouldBeFalse();
  }

  [Fact]
  public async Task Handle_PartiallyOccupied_DerivesUsedAndFree()
  {
    var lot = LotWithSpaces(general: 5, reserved: 0);
    GivenLot(lot);
    GivenSessionCounts(general: 2, reserved: 0);

    var result = await HandleAsync(lot);

    result.Value.GeneralCapacity.ShouldBe(5);
    result.Value.GeneralUsed.ShouldBe(2);
    result.Value.GeneralFree.ShouldBe(3);
  }

  [Fact]
  public async Task Handle_ExactlyFull_FlagsFullButNotOverCapacity()
  {
    var lot = LotWithSpaces(general: 2, reserved: 0);
    GivenLot(lot);
    GivenSessionCounts(general: 2, reserved: 0);

    var result = await HandleAsync(lot);

    result.Value.GeneralFree.ShouldBe(0);
    result.Value.IsGeneralPoolFull.ShouldBeTrue();
    result.Value.IsOverCapacity.ShouldBeFalse();
  }

  [Fact]
  public async Task Handle_MoreSessionsThanCapacity_FloorsFreeAtZeroAndFlagsOverCapacity()
  {
    var lot = LotWithSpaces(general: 2, reserved: 0);
    GivenLot(lot);
    GivenSessionCounts(general: 5, reserved: 0);

    var result = await HandleAsync(lot);

    result.Value.GeneralUsed.ShouldBe(5);
    result.Value.GeneralFree.ShouldBe(0);
    result.Value.IsOverCapacity.ShouldBeTrue();
  }

  [Fact]
  public async Task Handle_DeactivatedGeneralSpace_DropsCapacity()
  {
    var lot = LotWithSpaces(general: 3, reserved: 0);
    lot.DeactivateSpace(lot.Spaces.First().Id, actorId: null);
    GivenLot(lot);
    GivenSessionCounts(general: 0, reserved: 0);

    var result = await HandleAsync(lot);

    result.Value.GeneralCapacity.ShouldBe(2);
    result.Value.GeneralFree.ShouldBe(2);
  }

  [Fact]
  public async Task Handle_ReservedSpaceCount_CountsOnlyActiveReservedSpaces()
  {
    var lot = LotWithSpaces(general: 4, reserved: 3);
    lot.DeactivateSpace(lot.Spaces.First(space => space.Type == SpaceType.Reserved).Id, actorId: null);
    GivenLot(lot);
    GivenSessionCounts(general: 0, reserved: 0);

    var result = await HandleAsync(lot);

    result.Value.ReservedSpaceCount.ShouldBe(2);
    result.Value.GeneralCapacity.ShouldBe(4);
  }

  [Fact]
  public async Task Handle_ReservedSessions_DoNotConsumeTheGeneralPool()
  {
    var lot = LotWithSpaces(general: 2, reserved: 2);
    GivenLot(lot);
    GivenSessionCounts(general: 1, reserved: 2);

    var result = await HandleAsync(lot);

    result.Value.ReservedOccupied.ShouldBe(2);
    result.Value.GeneralUsed.ShouldBe(1);
    result.Value.GeneralFree.ShouldBe(1);
    result.Value.IsGeneralPoolFull.ShouldBeFalse();
  }

  [Fact]
  public async Task Handle_StampsAsOf()
  {
    var lot = LotWithSpaces(general: 1, reserved: 0);
    GivenLot(lot);
    GivenSessionCounts(general: 0, reserved: 0);
    var before = DateTimeOffset.UtcNow;

    var result = await HandleAsync(lot);

    result.Value.AsOf.ShouldBeGreaterThanOrEqualTo(before);
    result.Value.AsOf.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow);
  }
}
