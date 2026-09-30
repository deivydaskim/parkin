using Ardalis.GuardClauses;

namespace Parkin.Api.Domain.AccessEventAggregate;

public sealed record AccessEventActor
{
  private AccessEventActor(Guid id, bool isStaff)
  {
    Id = Guard.Against.Default(id, nameof(id));
    IsStaff = isStaff;
  }

  public Guid Id { get; }
  public bool IsStaff { get; }
  public Guid? StaffId => IsStaff ? Id : null;

  public static AccessEventActor ApiKey(Guid apiKeyId) => new(apiKeyId, isStaff: false);

  public static AccessEventActor Staff(Guid staffId) => new(staffId, isStaff: true);
}
