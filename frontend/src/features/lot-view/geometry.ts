import type { LotLayout } from '@/features/lots/schemas'
import type { SpacePlacement } from '@/features/spaces/schemas'

export type Point = { x: number; y: number }

export type Bounds = {
  minX: number
  minY: number
  maxX: number
  maxY: number
}

export type PlacedBay = { id: string; placement: SpacePlacement }

const EDGE_TOLERANCE = 0.01

export const degreesToRadians = (degrees: number) => (degrees * Math.PI) / 180

export function normalizeDegrees(degrees: number) {
  const normalized = ((degrees % 360) + 360) % 360
  return roundTo(normalized === 360 ? 0 : normalized, 2)
}

export function roundTo(value: number, decimals: number) {
  const factor = 10 ** decimals
  return Math.round(value * factor) / factor
}

export function bayCorners(placement: SpacePlacement): Point[] {
  const angle = degreesToRadians(placement.rotationDegrees)
  const cos = Math.cos(angle)
  const sin = Math.sin(angle)
  const halfWidth = placement.width / 2
  const halfLength = placement.length / 2
  const local: Point[] = [
    { x: -halfWidth, y: -halfLength },
    { x: halfWidth, y: -halfLength },
    { x: halfWidth, y: halfLength },
    { x: -halfWidth, y: halfLength },
  ]
  return local.map((corner) => ({
    x: placement.x + corner.x * cos - corner.y * sin,
    y: placement.y + corner.x * sin + corner.y * cos,
  }))
}

export function boundsOf(placements: SpacePlacement[]): Bounds | null {
  if (placements.length === 0) return null
  const corners = placements.flatMap(bayCorners)
  return {
    minX: Math.min(...corners.map((c) => c.x)),
    minY: Math.min(...corners.map((c) => c.y)),
    maxX: Math.max(...corners.map((c) => c.x)),
    maxY: Math.max(...corners.map((c) => c.y)),
  }
}

export function sceneBounds(
  layout: LotLayout | null,
  placements: SpacePlacement[],
  margin = 4,
): Bounds {
  if (layout) {
    return {
      minX: 0,
      minY: 0,
      maxX: layout.widthMeters,
      maxY: layout.lengthMeters,
    }
  }
  const bays = boundsOf(placements)
  if (!bays) return { minX: 0, minY: 0, maxX: 30, maxY: 20 }
  return {
    minX: Math.min(0, bays.minX - margin),
    minY: Math.min(0, bays.minY - margin),
    maxX: bays.maxX + margin,
    maxY: bays.maxY + margin,
  }
}

export function isOutsideFootprint(
  placement: SpacePlacement,
  layout: LotLayout,
) {
  if (placement.level >= layout.levelCount) return true
  return bayCorners(placement).some(
    (corner) =>
      corner.x < -EDGE_TOLERANCE ||
      corner.y < -EDGE_TOLERANCE ||
      corner.x > layout.widthMeters + EDGE_TOLERANCE ||
      corner.y > layout.lengthMeters + EDGE_TOLERANCE,
  )
}

function projectOnto(corners: Point[], axis: Point) {
  const values = corners.map((c) => c.x * axis.x + c.y * axis.y)
  return { min: Math.min(...values), max: Math.max(...values) }
}

function edgeNormals(corners: Point[]): Point[] {
  return [0, 1].map((index) => {
    const a = corners[index]
    const b = corners[index + 1]
    const length = Math.hypot(b.x - a.x, b.y - a.y) || 1
    return { x: -(b.y - a.y) / length, y: (b.x - a.x) / length }
  })
}

export function baysOverlap(first: SpacePlacement, second: SpacePlacement) {
  if (first.level !== second.level) return false
  const a = bayCorners(first)
  const b = bayCorners(second)
  for (const axis of [...edgeNormals(a), ...edgeNormals(b)]) {
    const pa = projectOnto(a, axis)
    const pb = projectOnto(b, axis)
    if (
      pa.max - EDGE_TOLERANCE <= pb.min ||
      pb.max - EDGE_TOLERANCE <= pa.min
    ) {
      return false
    }
  }
  return true
}

export function findOverlaps(bays: PlacedBay[]): Array<[string, string]> {
  const withBounds = bays.map((bay) => ({
    ...bay,
    bounds: boundsOf([bay.placement])!,
  }))
  withBounds.sort((a, b) => a.bounds.minX - b.bounds.minX)
  const overlaps: Array<[string, string]> = []
  for (let i = 0; i < withBounds.length; i++) {
    const current = withBounds[i]
    for (let j = i + 1; j < withBounds.length; j++) {
      const other = withBounds[j]
      if (other.bounds.minX >= current.bounds.maxX) break
      if (
        other.bounds.minY >= current.bounds.maxY ||
        other.bounds.maxY <= current.bounds.minY
      ) {
        continue
      }
      if (baysOverlap(current.placement, other.placement)) {
        overlaps.push([current.id, other.id])
      }
    }
  }
  return overlaps
}

export function compareLabels(a: string, b: string) {
  return a.localeCompare(b, undefined, { numeric: true, sensitivity: 'base' })
}
