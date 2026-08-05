using NSubstitute;
using Parkin.Api;
using Parkin.Api.AuditFeatures;
using Parkin.Api.AuditFeatures.List;
using Parkin.Api.Domain.AuditAggregate;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.AuditFeatures.List;

public class ListAuditHandlerTests
{
  private readonly IListAuditQueryService _query = Substitute.For<IListAuditQueryService>();

  private ListAuditHandler CreateSut() => new(_query);

  [Fact]
  public async Task Handle_DefaultsPageAndPerPage_WhenNotProvided()
  {
    var expected = new PagedResult<AuditLogEntryDto>([], 1, Constants.DEFAULT_PAGE_SIZE, 0, 0);
    _query.ListAsync(1, Constants.DEFAULT_PAGE_SIZE, null, null, null, null, null).Returns(expected);

    var result = await CreateSut().Handle(
      new ListAuditQuery(null, null, null, null, null, null, null),
      CancellationToken.None);

    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldBe(expected);
  }

  [Fact]
  public async Task Handle_PassesAllFiltersThrough_ToQueryService()
  {
    var from = DateTimeOffset.UtcNow.AddDays(-7);
    var to = DateTimeOffset.UtcNow;
    var actorId = Guid.NewGuid();
    var expected = new PagedResult<AuditLogEntryDto>([], 2, 25, 0, 0);

    _query.ListAsync(2, 25, from, to, actorId, AuditActorType.Staff, AuditEntityTypes.Reservation)
      .Returns(expected);

    var result = await CreateSut().Handle(
      new ListAuditQuery(2, 25, from, to, actorId, AuditActorType.Staff, AuditEntityTypes.Reservation),
      CancellationToken.None);

    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldBe(expected);
    await _query.Received(1).ListAsync(2, 25, from, to, actorId, AuditActorType.Staff, AuditEntityTypes.Reservation);
  }
}
