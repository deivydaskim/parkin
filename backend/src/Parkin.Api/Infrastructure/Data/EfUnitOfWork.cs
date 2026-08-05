using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.Interfaces;

namespace Parkin.Api.Infrastructure.Data;

// Wraps AppDbContext.Database.BeginTransactionAsync/CommitAsync so callers can
// issue several SaveChangesAsync calls (each ordered, each visible to the next)
// while keeping the whole sequence invisible to other connections until commit.
// CreateExecutionStrategy makes this retry-safe if Npgsql resiliency is ever enabled.
public class EfUnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
  public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
  {
    var strategy = dbContext.Database.CreateExecutionStrategy();
    await strategy.ExecuteAsync(async () =>
    {
      await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
      await action(cancellationToken);
      await transaction.CommitAsync(cancellationToken);
    });
  }
}
