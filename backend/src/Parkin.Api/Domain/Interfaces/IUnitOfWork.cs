namespace Parkin.Api.Domain.Interfaces;

public interface IUnitOfWork
{
  Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken);
}
