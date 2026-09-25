import { describe, expect, it } from 'vitest'
import {
  bayRotation,
  defaultRow,
  generateLayout,
  generateRowPlacements,
  nextRowStart,
  rowGeometry,
  type ArrangeSpace,
} from './auto-arrange'
import { findOverlaps } from './geometry'

function spaces(count: number, prefix = 'S'): ArrangeSpace[] {
  return Array.from({ length: count }, (_, index) => ({
    id: `${prefix}-${index + 1}`,
    label: `${prefix}${index + 1}`,
    isPlaced: false,
  }))
}

describe('generateRowPlacements', () => {
  it('emits one placement per bay, spaced by bay width for perpendicular bays', () => {
    const placements = generateRowPlacements(defaultRow({ bayCount: 4 }), {
      x: 2,
      y: 3,
    })

    expect(placements).toHaveLength(4)
    expect(placements.map((p) => p.x)).toEqual([2, 4.5, 7, 9.5])
    expect(placements.every((p) => p.y === 3)).toBe(true)
    expect(placements.every((p) => p.rotationDegrees === 0)).toBe(true)
  })

  it('follows the row direction', () => {
    const placements = generateRowPlacements(
      defaultRow({ bayCount: 3, directionDegrees: 90 }),
      { x: 10, y: 0 },
    )

    expect(placements.map((p) => [p.x, p.y])).toEqual([
      [10, 0],
      [10, 2.5],
      [10, 5],
    ])
    expect(placements[0].rotationDegrees).toBe(90)
  })

  it('widens the pitch for angled bays', () => {
    const row = defaultRow({ bayCount: 2, bayAngle: 45 })
    const placements = generateRowPlacements(row, { x: 0, y: 0 })

    expect(placements[1].x).toBeCloseTo(2.5 / Math.sin(Math.PI / 4), 2)
    expect(rowGeometry(row).depth).toBeCloseTo(
      5 * Math.SQRT1_2 + 2.5 * Math.SQRT1_2,
      5,
    )
  })

  it('rotates angled and flipped bays', () => {
    expect(
      bayRotation({ directionDegrees: 0, bayAngle: 90, flip: false }),
    ).toBe(0)
    expect(bayRotation({ directionDegrees: 0, bayAngle: 90, flip: true })).toBe(
      180,
    )
    expect(
      bayRotation({ directionDegrees: 0, bayAngle: 60, flip: false }),
    ).toBe(330)
    expect(bayRotation({ directionDegrees: 0, bayAngle: 45, flip: true })).toBe(
      225,
    )
    expect(
      bayRotation({ directionDegrees: 90, bayAngle: 90, flip: false }),
    ).toBe(90)
  })

  it('propagates level and bay size to every placement', () => {
    const placements = generateRowPlacements(
      defaultRow({ bayCount: 3, level: 2, bayWidth: 2.7, bayLength: 5.5 }),
      { x: 0, y: 0 },
    )

    expect(
      placements.every(
        (p) => p.level === 2 && p.width === 2.7 && p.length === 5.5,
      ),
    ).toBe(true)
  })
})

describe('nextRowStart', () => {
  it('offsets the next row by both half depths plus the aisle', () => {
    const row = defaultRow({ aisleAfter: 6 })

    expect(nextRowStart(row, { x: 1, y: 2.5 }, row)).toEqual({ x: 1, y: 13.5 })
  })

  it('packs back-to-back rows with no aisle', () => {
    const row = defaultRow({ aisleAfter: 0 })

    expect(nextRowStart(row, { x: 0, y: 2.5 }, row)).toEqual({ x: 0, y: 7.5 })
  })
})

describe('generateLayout', () => {
  it('lays out a 2-row, 40-bay lot in label order with no overlaps', () => {
    const rows = [
      defaultRow({ start: { x: 3, y: 3.5 }, bayCount: 20, zone: 'North' }),
      defaultRow({ bayCount: 20, flip: true, zone: 'South' }),
    ]

    const result = generateLayout(rows, spaces(40), { onlyUnplaced: false })

    expect(result.changes).toHaveLength(40)
    expect(result.rows.map((r) => r.assigned)).toEqual([20, 20])
    expect(result.rows[1].start).toEqual({ x: 3, y: 15 })
    expect(result.changes[0]).toMatchObject({ spaceId: 'S-1', zone: 'North' })
    expect(result.changes[20]).toMatchObject({ spaceId: 'S-21', zone: 'South' })
    expect(result.changes[20].placement?.rotationDegrees).toBe(180)
    const overlaps = findOverlaps(
      result.changes.map((c) => ({ id: c.spaceId, placement: c.placement! })),
    )
    expect(overlaps).toEqual([])
  })

  it('sorts labels naturally', () => {
    const pool: ArrangeSpace[] = [
      { id: 'b', label: 'A10', isPlaced: false },
      { id: 'a', label: 'A2', isPlaced: false },
    ]

    const result = generateLayout(
      [defaultRow({ bayCount: 2, start: { x: 0, y: 0 } })],
      pool,
      {
        onlyUnplaced: false,
      },
    )

    expect(result.changes.map((c) => c.spaceId)).toEqual(['a', 'b'])
  })

  it('fills rows from a label filter subset', () => {
    const pool = [...spaces(3, 'A'), ...spaces(3, 'B')]

    const result = generateLayout(
      [
        defaultRow({
          bayCount: 5,
          start: { x: 0, y: 0 },
          source: { mode: 'filter', labelFilter: 'b' },
        }),
      ],
      pool,
      { onlyUnplaced: false },
    )

    expect(result.changes.map((c) => c.spaceId)).toEqual(['B-1', 'B-2', 'B-3'])
    expect(result.rows[0]).toMatchObject({ requested: 5, assigned: 3 })
  })

  it('skips placed spaces when only unplaced are requested', () => {
    const pool = spaces(3).map((space, index) => ({
      ...space,
      isPlaced: index === 0,
    }))

    const result = generateLayout(
      [defaultRow({ bayCount: 3, start: { x: 0, y: 0 } })],
      pool,
      {
        onlyUnplaced: true,
      },
    )

    expect(result.changes.map((c) => c.spaceId)).toEqual(['S-2', 'S-3'])
  })

  it('never assigns a space twice across rows', () => {
    const result = generateLayout(
      [
        defaultRow({ bayCount: 2, start: { x: 0, y: 0 } }),
        defaultRow({ bayCount: 2 }),
      ],
      spaces(3),
      { onlyUnplaced: false },
    )

    const ids = result.changes.map((c) => c.spaceId)
    expect(new Set(ids).size).toBe(ids.length)
    expect(result.rows.map((r) => r.assigned)).toEqual([2, 1])
  })

  it('omits a blank zone so existing zones are kept', () => {
    const result = generateLayout(
      [defaultRow({ bayCount: 1, start: { x: 0, y: 0 }, zone: '  ' })],
      spaces(1),
      {
        onlyUnplaced: false,
      },
    )

    expect(result.changes[0].zone).toBeUndefined()
  })
})
