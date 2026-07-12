namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

// No-op placeholder: the source-gen Mediator requires every notification to have at least
// one registered handler. Wire up real consumers (e.g. audit log) as they're built.
public class LotArchivedEventHandler : INotificationHandler<LotArchivedEvent>
{
  public ValueTask Handle(LotArchivedEvent notification, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
