using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Parkin.Api.Infrastructure.Data.Config;

internal class VogenGuidIdValueGenerator<TContext, TEntityBase, TId> : ValueGenerator<TId>
    where TContext : DbContext
    where TEntityBase : EntityBase<TEntityBase, TId>
    where TId : struct
{
  public override TId Next(EntityEntry entry)
  {
    var fromMethod = typeof(TId).GetMethod("From", new[] { typeof(Guid) });
    if (fromMethod == null)
    {
      throw new InvalidOperationException($"Type {typeof(TId).Name} does not have a From(Guid) method");
    }

    return (TId)fromMethod.Invoke(null, new object[] { Guid.CreateVersion7() })!;
  }

  public override bool GeneratesTemporaryValues => false;
}
