import {
  Decision,
  type AccessEventDecision,
  type DenyReason,
  type Direction as DirectionValue,
} from './schemas'

export const denyReasonLabels: Record<DenyReason, string> = {
  NotAuthorized: 'Not authorized for this lot',
  LotFull: 'Lot is full',
  NoOpenSession: 'No open session for this plate',
  LotArchived: 'Lot is archived',
  LotNotFound: 'Lot not found',
}

export const directionLabels: Record<DirectionValue, string> = {
  Enter: 'Entry',
  Exit: 'Exit',
}

type DecisionSummary = Pick<
  AccessEventDecision,
  'decision' | 'reason' | 'pool' | 'reservedSpaceLabel'
>

export function describeOutcome(decision: DecisionSummary) {
  if (decision.decision === Decision.Deny) {
    return decision.reason ? denyReasonLabels[decision.reason] : 'Denied'
  }

  if (decision.reservedSpaceLabel) {
    return `Reserved space ${decision.reservedSpaceLabel}`
  }

  return decision.pool ? `${decision.pool} pool` : 'Allowed'
}
