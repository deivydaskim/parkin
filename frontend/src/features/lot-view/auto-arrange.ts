import type { SpacePlacement } from '@/features/spaces/schemas'
import {
  compareLabels,
  degreesToRadians,
  normalizeDegrees,
  roundTo,
  type Point,
} from './geometry'
import type { SpaceLayoutChange } from './schemas'

export const BAY_ANGLES = [90, 60, 45] as const
export type BayAngle = (typeof BAY_ANGLES)[number]

export type SpaceSource =
  { mode: 'labelOrder' } | { mode: 'filter'; labelFilter: string }

export type RowSpec = {
  start: Point | null
  directionDegrees: number
  bayCount: number
  bayWidth: number
  bayLength: number
  bayAngle: BayAngle
  flip: boolean
  aisleAfter: number
  level: number
  zone: string
  source: SpaceSource
}

export type ArrangeSpace = {
  id: string
  label: string
  isPlaced: boolean
}

export type ArrangeOptions = {
  onlyUnplaced: boolean
}

export type RowResult = {
  start: Point
  requested: number
  assigned: number
}

export type ArrangeResult = {
  changes: SpaceLayoutChange[]
  rows: RowResult[]
}

export function rowGeometry(
  row: Pick<RowSpec, 'bayWidth' | 'bayLength' | 'bayAngle'>,
) {
  const angle = degreesToRadians(row.bayAngle)
  return {
    pitch: row.bayWidth / Math.sin(angle),
    depth:
      row.bayLength * Math.sin(angle) +
      row.bayWidth * Math.abs(Math.cos(angle)),
  }
}

export function bayRotation(
  row: Pick<RowSpec, 'directionDegrees' | 'bayAngle' | 'flip'>,
) {
  return normalizeDegrees(
    row.flip
      ? row.directionDegrees - row.bayAngle - 90
      : row.directionDegrees + row.bayAngle - 90,
  )
}

export function generateRowPlacements(
  row: Omit<RowSpec, 'start' | 'source' | 'zone' | 'aisleAfter'>,
  start: Point,
  count = row.bayCount,
): SpacePlacement[] {
  const direction = degreesToRadians(row.directionDegrees)
  const { pitch } = rowGeometry(row)
  const rotationDegrees = bayRotation(row)

  return Array.from({ length: Math.max(0, count) }, (_, index) => ({
    x: roundTo(start.x + Math.cos(direction) * pitch * index, 2),
    y: roundTo(start.y + Math.sin(direction) * pitch * index, 2),
    rotationDegrees,
    level: row.level,
    width: row.bayWidth,
    length: row.bayLength,
  }))
}

export function nextRowStart(
  previous: RowSpec,
  previousStart: Point,
  next: RowSpec,
): Point {
  const direction = degreesToRadians(previous.directionDegrees)
  const offset =
    rowGeometry(previous).depth / 2 +
    previous.aisleAfter +
    rowGeometry(next).depth / 2
  return {
    x: roundTo(previousStart.x - Math.sin(direction) * offset, 2),
    y: roundTo(previousStart.y + Math.cos(direction) * offset, 2),
  }
}

function takeSpaces(
  pool: ArrangeSpace[],
  used: Set<string>,
  source: SpaceSource,
  count: number,
) {
  const filter =
    source.mode === 'filter' ? source.labelFilter.trim().toLowerCase() : ''
  return pool
    .filter((space) => !used.has(space.id))
    .filter((space) => !filter || space.label.toLowerCase().includes(filter))
    .slice(0, Math.max(0, count))
}

export function generateLayout(
  rows: RowSpec[],
  spaces: ArrangeSpace[],
  options: ArrangeOptions,
): ArrangeResult {
  const pool = spaces
    .filter((space) => !options.onlyUnplaced || !space.isPlaced)
    .sort((a, b) => compareLabels(a.label, b.label))
  const used = new Set<string>()
  const changes: SpaceLayoutChange[] = []
  const results: RowResult[] = []

  rows.forEach((row, index) => {
    const start =
      row.start ??
      (index === 0
        ? { x: 0, y: 0 }
        : nextRowStart(rows[index - 1], results[index - 1].start, row))
    const assigned = takeSpaces(pool, used, row.source, row.bayCount)
    const placements = generateRowPlacements(row, start, assigned.length)

    assigned.forEach((space, bayIndex) => {
      used.add(space.id)
      changes.push({
        spaceId: space.id,
        placement: placements[bayIndex],
        zone: row.zone.trim() || undefined,
      })
    })

    results.push({ start, requested: row.bayCount, assigned: assigned.length })
  })

  return { changes, rows: results }
}

export function defaultRow(overrides: Partial<RowSpec> = {}): RowSpec {
  return {
    start: null,
    directionDegrees: 0,
    bayCount: 20,
    bayWidth: 2.5,
    bayLength: 5,
    bayAngle: 90,
    flip: false,
    aisleAfter: 6.5,
    level: 0,
    zone: '',
    source: { mode: 'labelOrder' },
    ...overrides,
  }
}
