import type { SpacePlacement } from '@/features/spaces/schemas'
import { bayStyleOf, type BayStyle, type LayoutSpace } from './schemas'

export const LEVEL_HEIGHT = 3.2
export const LABELS_AUTO_LIMIT = 150

export type SceneBay = {
  id: string
  label: string
  style: BayStyle
  placement: SpacePlacement
  isPreview: boolean
  assignee: string | null
}

export type LevelFilter = 'all' | number

export type ScenePalette = {
  background: string
  ground: string
  slab: string
  curb: string
  paint: string
  general: string
  reservedAssigned: string
  reservedUnassigned: string
  inactive: string
  hover: string
  selected: string
  preview: string
  marker: string
}

export const lightPalette: ScenePalette = {
  background: '#eef2f6',
  ground: '#b8bec7',
  slab: '#cfd4db',
  curb: '#8a929e',
  paint: '#ffffff',
  general: '#7c8da3',
  reservedAssigned: '#e69500',
  reservedUnassigned: '#f3cf7a',
  inactive: '#9aa0a8',
  hover: '#19a7e0',
  selected: '#1d4ed8',
  preview: '#14b8a6',
  marker: '#1f2937',
}

export const darkPalette: ScenePalette = {
  background: '#0f1115',
  ground: '#2b2f36',
  slab: '#3a3f47',
  curb: '#5b626d',
  paint: '#e5e7eb',
  general: '#51647d',
  reservedAssigned: '#f59e0b',
  reservedUnassigned: '#8a6a24',
  inactive: '#4b4f56',
  hover: '#38bdf8',
  selected: '#60a5fa',
  preview: '#2dd4bf',
  marker: '#f3f4f6',
}

export function toSceneBays(
  spaces: LayoutSpace[],
  overrides: ReadonlyMap<string, SpacePlacement>,
): SceneBay[] {
  return spaces.flatMap((space) => {
    const override = overrides.get(space.id)
    const placement = override ?? space.placement
    if (!placement) return []
    return [
      {
        id: space.id,
        label: space.label,
        style: bayStyleOf(space),
        placement,
        isPreview: override !== undefined,
        assignee: space.reservation?.driverName ?? null,
      },
    ]
  })
}

export function levelsOf(
  levelCount: number | undefined,
  bays: SceneBay[],
): number[] {
  const highest = Math.max(
    (levelCount ?? 1) - 1,
    ...bays.map((bay) => bay.placement.level),
    0,
  )
  return Array.from({ length: highest + 1 }, (_, level) => level)
}

export function isLevelVisible(level: number, filter: LevelFilter) {
  return filter === 'all' || filter === level
}

export function levelName(level: number) {
  return level === 0 ? 'Ground' : `Level ${level}`
}
