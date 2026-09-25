using Ardalis.SharedKernel;
using NSubstitute;
using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.AccessEventAggregate.Events;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.AccessEventFeatures;

public class AccessEventRecordedEventHandlerTests
{
  private readonly IRepository<AuditLogEntry> _auditRepository = Substitute.For<IRepository<AuditLogEntry>>();

  private static AccessEvent RecordEvent(EventSource source, Guid? actorId, Guid? actingStaffId) =>
    AccessEvent.Record(ParkingLotId.From(Guid.NewGuid()), "AAA 111", "AAA111", matchedPlateId: null,
      matchedDriverId: null, Direction.Enter, source, Decision.Allow, denyReason: null, DateTimeOffset.UtcNow,
      "key-1", actorId, actingStaffId);

  private async Task<AuditLogEntry> AuditAsync(AccessEvent accessEvent, Guid? actorId)
  {
    AuditLogEntry? audited = null;
    await _auditRepository.AddAsync(Arg.Do<AuditLogEntry>(e => audited = e), Arg.Any<CancellationToken>());

    await new AccessEventRecordedEventHandler(_auditRepository)
      .Handle(new AccessEventRecordedEvent(accessEvent, actorId), CancellationToken.None);

    audited.ShouldNotBeNull();
    return audited;
  }

  [Fact]
  public async Task Handle_ManualEvent_AttributesToTheActingStaffMember()
  {
    var staffId = Guid.NewGuid();

    var audited = await AuditAsync(RecordEvent(EventSource.Manual, staffId, staffId), staffId);

    audited.ActorType.ShouldBe(AuditActorType.Staff);
    audited.ActorId.ShouldBe(staffId);
    audited.Action.ShouldBe(AuditActions.AccessEventIngested);
  }

  [Fact]
  public async Task Handle_GateEvent_AttributesToTheApiKey()
  {
    var apiKeyId = Guid.NewGuid();

    var audited = await AuditAsync(RecordEvent(EventSource.Lpr, apiKeyId, actingStaffId: null), apiKeyId);

    audited.ActorType.ShouldBe(AuditActorType.Api);
    audited.ActorId.ShouldBe(apiKeyId);
  }
}
