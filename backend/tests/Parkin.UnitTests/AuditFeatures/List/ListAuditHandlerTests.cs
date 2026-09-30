using NSubstitute;
using Parkin.Api;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Features.Audit;
using Parkin.Api.Features.Audit.List;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.AuditFeatures.List;

public class ListAuditHandlerTests
{
  private readonly IListAuditQueryService _query = Substitute.For<IListAuditQueryService>();

  private ListAuditHandler CreateSut() => new(_query);

  [Fact]
  public async Task Handle_PassesFilterAndCancellationToken_ToQueryService()
  {
    using var cts = new CancellationTokenSource();
    var filter = new AuditLogFilter(2, 25, DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow, Guid.NewGuid(),
      AuditActorType.Staff, AuditEntityTypes.Reservation);
    var expected = new PagedResult<AuditLogEntryResponse>([], 2, 25, 0, 0);
    _query.ListAsync(filter, cts.Token).Returns(expected);

    var result = await CreateSut().Handle(new ListAuditQuery(filter), cts.Token);

    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldBe(expected);
    await _query.Received(1).ListAsync(filter, cts.Token);
  }

  [Fact]
  public async Task Handle_ReturnsEmptyPage_WhenNothingMatches()
  {
    var filter = new AuditLogFilter(1, Constants.DEFAULT_PAGE_SIZE, null, null, null, null, null);
    var expected = new PagedResult<AuditLogEntryResponse>([], 1, Constants.DEFAULT_PAGE_SIZE, 0, 0);
    _query.ListAsync(filter, Arg.Any<CancellationToken>()).Returns(expected);

    var result = await CreateSut().Handle(new ListAuditQuery(filter), CancellationToken.None);

    result.IsSuccess.ShouldBeTrue();
    result.Value.Items.ShouldBeEmpty();
  }
}
