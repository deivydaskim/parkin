using Ardalis.Result;
using Ardalis.SharedKernel;
using NSubstitute;
using Parkin.Api.Domain.AccessGrantAggregate;
using Parkin.Api.Domain.AccessGrantAggregate.Specifications;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Features.Grants.Create;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.GrantFeatures.Create;

public class CreateGrantHandlerTests
{
  private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

  private readonly IReadRepository<Driver> _driverRepository = Substitute.For<IReadRepository<Driver>>();
  private readonly IReadRepository<ParkingLot> _lotRepository = Substitute.For<IReadRepository<ParkingLot>>();
  private readonly IRepository<AccessGrant> _grantRepository = Substitute.For<IRepository<AccessGrant>>();
  private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
  private readonly ParkingLot _lot = ParkingLot.Create("Grant Lot", "Europe/Vilnius");
  private readonly DriverId _driverId = DriverId.From(Guid.NewGuid());

  public CreateGrantHandlerTests()
  {
    _timeProvider.GetUtcNow().Returns(Now);
    _driverRepository.AnyAsync(Arg.Any<DriverByIdSpec>(), Arg.Any<CancellationToken>()).Returns(true);
    _lotRepository.GetByIdAsync(_lot.Id, Arg.Any<CancellationToken>()).Returns(_lot);
    _grantRepository.AnyAsync(Arg.Any<ActiveGrantForDriverLotSpec>(), Arg.Any<CancellationToken>()).Returns(false);
  }

  private CreateGrantHandler CreateSut() => new(_driverRepository, _lotRepository, _grantRepository, _timeProvider);

  private CreateGrantCommand Command(DateTimeOffset? validFrom = null, DateTimeOffset? validTo = null) =>
    new(_driverId, _lot.Id, validFrom, validTo, null);

  [Fact]
  public async Task Handle_WithoutValidFrom_StartsGrantAtCurrentTime()
  {
    var result = await CreateSut().Handle(Command(), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Ok);
    result.Value.ValidFrom.ShouldBe(Now);
    result.Value.ParkingLotName.ShouldBe(_lot.Name);
    await _grantRepository.Received(1).AddAsync(
      Arg.Is<AccessGrant>(grant => grant.CreatedAt == Now), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ValidToAlreadyPassedWithoutValidFrom_ReturnsInvalidWithoutSaving()
  {
    var result = await CreateSut().Handle(Command(validTo: Now.AddDays(-1)), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Invalid);
    await _grantRepository.DidNotReceive().AddAsync(Arg.Any<AccessGrant>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ActiveGrantAlreadyExists_ReturnsInvalid()
  {
    _grantRepository.AnyAsync(Arg.Any<ActiveGrantForDriverLotSpec>(), Arg.Any<CancellationToken>()).Returns(true);

    var result = await CreateSut().Handle(Command(), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Invalid);
  }

  [Fact]
  public async Task Handle_DriverNotFound_ReturnsNotFound()
  {
    _driverRepository.AnyAsync(Arg.Any<DriverByIdSpec>(), Arg.Any<CancellationToken>()).Returns(false);

    var result = await CreateSut().Handle(Command(), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.NotFound);
  }
}
