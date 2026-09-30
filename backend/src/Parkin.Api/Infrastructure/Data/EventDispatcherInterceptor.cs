using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Parkin.Api.Infrastructure.Data;

public class EventDispatchInterceptor(IDomainEventDispatcher domainEventDispatcher) : SaveChangesInterceptor
{
  public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
    CancellationToken cancellationToken = default)
  {
    if (eventData.Context is not null)
    {
      var entitiesWithEvents = eventData.Context.ChangeTracker.Entries<HasDomainEventsBase>()
        .Select(entry => entry.Entity)
        .Where(entity => entity.DomainEvents.Any())
        .ToArray();

      await domainEventDispatcher.DispatchAndClearEvents(entitiesWithEvents);
    }

    return await base.SavedChangesAsync(eventData, result, cancellationToken);
  }
}
