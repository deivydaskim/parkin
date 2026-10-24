namespace Parkin.Api.Domain.AccessEventAggregate;

public enum DenyReason
{
  NotAuthorized,
  LotFull,
  NoOpenSession,
  AlreadyParked,
  LotArchived,
  LotNotFound
}
