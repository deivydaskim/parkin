namespace Parkin.Api.Domain.AccessEventAggregate;

// Superset of Domain.Services.DecisionReason, which only covers what the decision engine decides.
// LotNotFound is response-only: access_events.lot_id is a non-null FK, so it is never persisted.
public enum DenyReason
{
  NotAuthorized,
  LotFull,
  NoOpenSession,
  LotArchived,
  LotNotFound
}
