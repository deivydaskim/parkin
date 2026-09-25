import { describe, expect, it } from 'vitest'
import type { SpacePlacement } from '@/features/spaces/schemas'
import {
  baysOverlap,
  bayCorners,
  findOverlaps,
  isOutsideFootprint,
  normalizeDegrees,
  sceneBounds,
} from './geometry'

const bay = (overrides: Partial<SpacePlacement> = {}): SpacePlacement => ({
  x: 5,
  y: 5,
  rotationDegrees: 0,
  level: 0,
  width: 2.5,
  length: 5,
  ...overrides,
})

describe('geometry', () => {
  it('computes rotated corners', () => {
    const corners = bayCorners(bay({ x: 0, y: 0, rotationDegrees: 90 }))

    expect(corners[0].x).toBeCloseTo(2.5)
    expect(corners[0].y).toBeCloseTo(-1.25)
  })

  it('treats adjacent bays as not overlapping', () => {
    expect(baysOverlap(bay(), bay({ x: 7.5 }))).toBe(false)
  })

  it('detects overlapping and rotated overlaps', () => {
    expect(baysOverlap(bay(), bay({ x: 6 }))).toBe(true)
    expect(baysOverlap(bay(), bay({ x: 7, rotationDegrees: 90 }))).toBe(true)
  })

  it('ignores bays on other levels', () => {
    expect(baysOverlap(bay(), bay({ level: 1 }))).toBe(false)
  })

  it('lists overlapping pairs', () => {
    const overlaps = findOverlaps([
      { id: 'a', placement: bay() },
      { id: 'b', placement: bay({ x: 6 }) },
      { id: 'c', placement: bay({ x: 20 }) },
    ])

    expect(overlaps).toEqual([['a', 'b']])
  })

  it('flags bays poking out of the footprint or on missing levels', () => {
    const layout = { widthMeters: 10, lengthMeters: 10, levelCount: 1 }

    expect(isOutsideFootprint(bay(), layout)).toBe(false)
    expect(isOutsideFootprint(bay({ x: 9.5 }), layout)).toBe(true)
    expect(isOutsideFootprint(bay({ level: 1 }), layout)).toBe(true)
  })

  it('normalizes rotation into [0, 360)', () => {
    expect(normalizeDegrees(-90)).toBe(270)
    expect(normalizeDegrees(360)).toBe(0)
    expect(normalizeDegrees(375)).toBe(15)
  })

  it('derives scene bounds from the layout, else from placed bays', () => {
    expect(
      sceneBounds({ widthMeters: 40, lengthMeters: 20, levelCount: 1 }, []),
    ).toEqual({
      minX: 0,
      minY: 0,
      maxX: 40,
      maxY: 20,
    })
    expect(sceneBounds(null, [bay({ x: 10, y: 10 })], 2)).toEqual({
      minX: 0,
      minY: 0,
      maxX: 13.25,
      maxY: 14.5,
    })
  })
})
