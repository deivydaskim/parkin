namespace Parkin.Api.Domain.Interfaces;

// Runs `action` inside a single ambient DB transaction, wrapping multiple
// SaveChangesAsync calls so intermediate statements are ordered and never
// visible to other connections until the whole action commits.
public interface IUnitOfWork
{
  Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken);
}
