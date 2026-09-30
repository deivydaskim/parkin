using Ardalis.Result;
using Parkin.Api.Domain.AccessGrantAggregate;
using Parkin.Api.Domain.AccessGrantAggregate.Events;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.Domain.AccessGrantAggregate;

public class AccessGrantTests
{
  private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
  private static readonly DriverId DriverId = DriverId.From(Guid.NewGuid());
  private static readonly ParkingLotId LotId = ParkingLotId.From(Guid.NewGuid());

  private static AccessGrant CreateOpenEnded() => AccessGrant.Create(DriverId, LotId, null, null, Now, null).Value;

  [Fact]
  public void Create_WithoutValidFrom_StartsNowAndStampsCreatedAt()
  {
    var actorId = Guid.NewGuid();

    var result = AccessGrant.Create(DriverId, LotId, null, Now.AddDays(5), Now, actorId);

    result.IsSuccess.ShouldBeTrue();
    var grant = result.Value;
    grant.ValidFrom.ShouldBe(Now);
    grant.ValidTo.ShouldBe(Now.AddDays(5));
    grant.CreatedAt.ShouldBe(Now);
    grant.CreatedBy.ShouldBe(actorId);
    grant.Status.ShouldBe(GrantStatus.Active);
    grant.DomainEvents.OfType<GrantCreatedEvent>().ShouldHaveSingleItem().GrantId.ShouldBe(grant.Id);
  }

  [Fact]
  public void Create_ValidToBeforeExplicitValidFrom_ReturnsInvalid()
  {
    var result = AccessGrant.Create(DriverId, LotId, Now, Now.AddDays(-1), Now, null);

    result.Status.ShouldBe(ResultStatus.Invalid);
    result.ValidationErrors.ShouldHaveSingleItem().Identifier.ShouldBe("ValidTo");
  }

  [Fact]
  public void Create_ValidToInThePastWithoutValidFrom_ReturnsInvalid()
  {
    var result = AccessGrant.Create(DriverId, LotId, null, Now.AddMinutes(-1), Now, null);

    result.Status.ShouldBe(ResultStatus.Invalid);
  }

  [Fact]
  public void IsActiveAsOf_RespectsWindowAndStatus()
  {
    var grant = AccessGrant.Create(DriverId, LotId, Now, Now.AddDays(1), Now, null).Value;

    grant.IsActiveAsOf(Now.AddMinutes(-1)).ShouldBeFalse();
    grant.IsActiveAsOf(Now.AddHours(1)).ShouldBeTrue();
    grant.IsActiveAsOf(Now.AddDays(2)).ShouldBeFalse();

    grant.Revoke(null);
    grant.IsActiveAsOf(Now.AddHours(1)).ShouldBeFalse();
  }

  [Fact]
  public void Revoke_Active_RevokesAndRegistersEvent()
  {
    var grant = CreateOpenEnded();

    var result = grant.Revoke(Guid.NewGuid());

    result.IsSuccess.ShouldBeTrue();
    grant.Status.ShouldBe(GrantStatus.Revoked);
    grant.DomainEvents.OfType<GrantRevokedEvent>().ShouldHaveSingleItem();
  }

  [Fact]
  public void Revoke_AlreadyRevoked_ReturnsInvalidWithoutNewEvent()
  {
    var grant = CreateOpenEnded();
    grant.Revoke(null);

    var result = grant.Revoke(null);

    result.Status.ShouldBe(ResultStatus.Invalid);
    grant.DomainEvents.OfType<GrantRevokedEvent>().Count().ShouldBe(1);
  }
}
