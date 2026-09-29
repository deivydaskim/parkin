import { describe, expect, it } from 'vitest'
import { describeDecision, describeOffline } from './decision-messages'
import type { AccessEventDecision } from './schemas'

function decision(
  overrides: Partial<AccessEventDecision>,
): AccessEventDecision {
  return {
    eventId: null,
    decision: 'Allow',
    reason: null,
    pool: null,
    reservedSpaceLabel: null,
    sessionId: null,
    occurredAt: '2026-01-01T10:00:00+00:00',
    ...overrides,
  }
}

describe('describeDecision', () => {
  it('welcomes a general entry', () => {
    const message = describeDecision(decision({ pool: 'General' }), 'Enter')
    expect(message.text).toBe('Welcome')
    expect(message.tone).toBe('success')
  })

  it('shows the reserved space for a reserved entry', () => {
    const message = describeDecision(
      decision({ pool: 'Reserved', reservedSpaceLabel: 'A13' }),
      'Enter',
    )
    expect(message.text).toBe('Reserved space A13')
  })

  it('says goodbye on an allowed exit', () => {
    const message = describeDecision(decision({ pool: 'General' }), 'Exit')
    expect(message.text).toBe('Goodbye')
  })

  it.each([
    ['NotAuthorized', 'Not authorized'],
    ['LotFull', 'Lot full'],
    ['NoOpenSession', 'No entry recorded'],
    ['LotArchived', 'Lot closed'],
    ['LotNotFound', 'Lot closed'],
  ] as const)('maps deny reason %s', (reason, text) => {
    const message = describeDecision(
      decision({ decision: 'Deny', reason }),
      'Enter',
    )
    expect(message.text).toBe(text)
    expect(message.tone).toBe('danger')
  })

  it('falls back when a denial has no reason', () => {
    const message = describeDecision(decision({ decision: 'Deny' }), 'Enter')
    expect(message.text).toBe('Access denied')
  })
})

describe('describeOffline', () => {
  it('describes fail-open', () => {
    expect(describeOffline('open').subtext).toContain('Fail-open')
  })

  it('describes fail-closed', () => {
    expect(describeOffline('closed').subtext).toContain('Fail-closed')
  })
})
