import { describe, expect, it } from 'vitest'
import type { AccessEventDecision } from '@/lib/schemas'
import {
  isInside,
  reduceVehiclesInside,
  type VehicleInside,
} from './gate-store'

function decision(
  overrides: Partial<AccessEventDecision>,
): AccessEventDecision {
  return {
    eventId: null,
    decision: 'Allow',
    reason: null,
    pool: 'General',
    reservedSpaceLabel: null,
    sessionId: null,
    occurredAt: '2026-01-01T10:00:00+00:00',
    ...overrides,
  }
}

const inside = (
  plate: string,
  enteredAt = '2026-01-01T09:00:00+00:00',
): VehicleInside => ({
  plate,
  pool: 'General',
  reservedSpaceLabel: null,
  enteredAt,
})

describe('reduceVehiclesInside', () => {
  it('adds a vehicle on an allowed entry', () => {
    const result = reduceVehiclesInside([], {
      direction: 'Enter',
      plate: 'ABC123',
      decision: decision({ pool: 'Reserved', reservedSpaceLabel: 'A13' }),
    })
    expect(result).toEqual([
      {
        plate: 'ABC123',
        pool: 'Reserved',
        reservedSpaceLabel: 'A13',
        enteredAt: '2026-01-01T10:00:00+00:00',
      },
    ])
  })

  it('ignores a denied entry', () => {
    const vehicles: VehicleInside[] = []
    const result = reduceVehiclesInside(vehicles, {
      direction: 'Enter',
      plate: 'ABC123',
      decision: decision({ decision: 'Deny', reason: 'LotFull' }),
    })
    expect(result).toBe(vehicles)
  })

  it('removes the vehicle on an allowed exit', () => {
    const result = reduceVehiclesInside([inside('ABC123'), inside('XYZ999')], {
      direction: 'Exit',
      plate: 'ABC123',
      decision: decision({}),
    })
    expect(result.map((vehicle) => vehicle.plate)).toEqual(['XYZ999'])
  })

  it('removes only the most recent session of a duplicated plate', () => {
    const first = inside('ABC123', '2026-01-01T08:00:00+00:00')
    const second = inside('ABC123', '2026-01-01T09:00:00+00:00')
    const result = reduceVehiclesInside([first, second], {
      direction: 'Exit',
      plate: 'ABC123',
      decision: decision({}),
    })
    expect(result).toEqual([first])
  })

  it('ignores a denied exit', () => {
    const vehicles = [inside('ABC123')]
    const result = reduceVehiclesInside(vehicles, {
      direction: 'Exit',
      plate: 'ABC123',
      decision: decision({ decision: 'Deny', reason: 'NoOpenSession' }),
    })
    expect(result).toBe(vehicles)
  })

  it('ignores an allowed exit for an unknown plate', () => {
    const vehicles = [inside('ABC123')]
    const result = reduceVehiclesInside(vehicles, {
      direction: 'Exit',
      plate: 'NOPE000',
      decision: decision({}),
    })
    expect(result).toBe(vehicles)
  })
})

describe('isInside', () => {
  it('detects a plate that is inside', () => {
    expect(isInside([inside('ABC123')], 'ABC123')).toBe(true)
    expect(isInside([inside('ABC123')], 'XYZ999')).toBe(false)
  })
})
