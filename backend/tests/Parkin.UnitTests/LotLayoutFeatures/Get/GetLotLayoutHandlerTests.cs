using Ardalis.Result;
using NSubstitute;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Features.LotLayouts;
using Parkin.Api.Features.LotLayouts.Get;
using Parkin.Api.Features.Lots;
using Parkin.Api.Features.Spaces;
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
      .Returns((LotLayoutViewResponse?)null);

    var result = await CreateSut().Handle(new GetLotLayoutQuery(lotId), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_KnownLot_ReturnsProjectionForThatLot()
  {
    var lotId = ParkingLotId.From(Guid.NewGuid());
    var reservation = new LotLayoutReservationResponse(Guid.NewGuid(), Guid.NewGuid(), "Ona");
    var view = new LotLayoutViewResponse(
      new LotLayoutLotResponse(lotId.Value, "Lot", LotStatus.Active, new LotLayoutResponse(40m, 30m, 1)),
      [
        new LotLayoutSpaceResponse(Guid.NewGuid(), "R1", SpaceType.Reserved, SpaceStatus.Active, "North",
          new SpacePlacementResponse(5m, 5m, 0m, 0, 2.5m, 5m), reservation),
        new LotLayoutSpaceResponse(Guid.NewGuid(), "X1", SpaceType.General, SpaceStatus.Inactive, null, null, null),
      ]);
    _queryService.GetAsync(lotId, Arg.Any<CancellationToken>()).Returns(view);

    var result = await CreateSut().Handle(new GetLotLayoutQuery(lotId), CancellationToken.None);

    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldBeSameAs(view);
  }
}
