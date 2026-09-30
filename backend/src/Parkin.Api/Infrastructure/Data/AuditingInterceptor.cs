using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Infrastructure.Data;

public class AuditingInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
  private readonly HashSet<AuditableDomainEvent> _audited = new(ReferenceEqualityComparer.Instance);

  public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
  {
    AddAuditEntries(eventData.Context);
    return base.SavingChanges(eventData, result);
  }

  public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
    InterceptionResult<int> result, CancellationToken cancellationToken = default)
  {
    AddAuditEntries(eventData.Context);
    return base.SavingChangesAsync(eventData, result, cancellationToken);
  }

  private void AddAuditEntries(DbContext? context)
  {
    if (context is null) return;

    var pendingEvents = context.ChangeTracker.Entries<HasDomainEventsBase>()
      .SelectMany(entry => entry.Entity.DomainEvents.OfType<AuditableDomainEvent>())
      .Where(_audited.Add)
      .ToList();

    var now = timeProvider.GetUtcNow();
    foreach (var domainEvent in pendingEvents)
    {
      context.Set<AuditLogEntry>().Add(AuditLogEntry.FromEvent(domainEvent, now));
    }
  }
}
