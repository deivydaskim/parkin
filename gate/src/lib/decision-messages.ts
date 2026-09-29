import type { AccessEventDecision, DenyReason, Direction } from '@/lib/schemas'

export type DisplayTone = 'success' | 'danger' | 'warning' | 'info'

export type DisplayMessage = {
  text: string
  subtext: string | null
  tone: DisplayTone
}

export type FailSafeMode = 'open' | 'closed'

const DENY_MESSAGES: Record<DenyReason, { text: string; subtext: string }> = {
  NotAuthorized: { text: 'Not authorized', subtext: 'Plate has no access' },
  LotFull: { text: 'Lot full', subtext: 'No free spaces' },
  NoOpenSession: {
    text: 'No entry recorded',
    subtext: 'No open session for this plate',
  },
  LotArchived: { text: 'Lot closed', subtext: 'Gate is not in service' },
  LotNotFound: { text: 'Lot closed', subtext: 'Unknown lot' },
}

export const DENY_REASON_LABELS: Record<DenyReason, string> = {
  NotAuthorized: 'Not authorized',
  LotFull: 'Lot full',
  NoOpenSession: 'No open session',
  LotArchived: 'Lot archived',
  LotNotFound: 'Lot not found',
}

export function describeDecision(
  decision: AccessEventDecision,
  direction: Direction,
): DisplayMessage {
  if (decision.decision === 'Deny') {
    const message = decision.reason
      ? DENY_MESSAGES[decision.reason]
      : { text: 'Access denied', subtext: '' }
    return {
      text: message.text,
      subtext: message.subtext || null,
      tone: 'danger',
    }
  }

  if (direction === 'Exit') {
    return { text: 'Goodbye', subtext: 'Safe travels', tone: 'success' }
  }

  if (decision.pool === 'Reserved' && decision.reservedSpaceLabel) {
    return {
      text: `Reserved space ${decision.reservedSpaceLabel}`,
      subtext: 'Welcome',
      tone: 'success',
    }
  }

  return {
    text: 'Welcome',
    subtext: decision.pool === 'General' ? 'General parking' : null,
    tone: 'success',
  }
}

export function describeOffline(failSafe: FailSafeMode): DisplayMessage {
  return {
    text: 'Gate offline',
    subtext:
      failSafe === 'open'
        ? 'Fail-open: barrier released'
        : 'Fail-closed: barrier locked',
    tone: 'warning',
  }
}
