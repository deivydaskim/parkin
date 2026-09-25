using Ardalis.Result;
using NSubstitute;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.LotLayoutFeatures;
using Parkin.Api.LotLayoutFeatures.Get;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.LotLayoutFeatures.Get;

public class GetLotLayoutHandlerTests
{
  private readonly ILotLayoutQueryService _queryService = Substitute.For<ILotLayoutQueryService>();

  private GetLotLayoutHandler CreateSut() => new(_queryService);

  [Fact]
  public async Task Handle_UnknownLot_ReturnsNotFound()
  {
    var lotId = ParkingLotId.From(Guid.NewGuid());
    _queryService.GetAsync(lotId, Arg.Any<CancellationToken>())
      .Returns((LotLayoutViewDto?)null);

    var result = await CreateSut().Handle(
      new GetLotLayoutQuery(lotId), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_KnownLot_ReturnsProjectionForThatLot()
  {
    var lotId = ParkingLotId.From(Guid.NewGuid());
    var reservation = new LotLayoutReservationDto(
      ReservationId.From(Guid.NewGuid()), DriverId.From(Guid.NewGuid()), "Ona");
    var view = new LotLayoutViewDto(lotId, "Lot", LotStatus.Active, LotLayout.Create(40m, 30m).Value,
    [
      new LotLayoutSpaceDto(ParkingSpaceId.From(Guid.NewGuid()), "R1", SpaceType.Reserved, SpaceStatus.Active,
        "North", SpacePlacement.Create(5m, 5m).Value, reservation),
      new LotLayoutSpaceDto(ParkingSpaceId.From(Guid.NewGuid()), "X1", SpaceType.General, SpaceStatus.Inactive,
        null, null, null),
    ]);
    _queryService.GetAsync(lotId, Arg.Any<CancellationToken>()).Returns(view);

    var result = await CreateSut().Handle(new GetLotLayoutQuery(lotId), CancellationToken.None);

    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldBeSameAs(view);
    result.Value.Spaces[0].Reservation!.DriverName.ShouldBe("Ona");
    result.Value.Spaces[1].Placement.ShouldBeNull();
  }
}
