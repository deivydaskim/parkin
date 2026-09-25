import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link } from '@tanstack/react-router'
import { LayoutGrid, Table2 } from 'lucide-react'
import { useTheme } from 'next-themes'
import { Button } from '@/components/ui/button'
import { RoleGate } from '@/features/auth/components/RoleGate'
import { OccupancyStats } from '@/features/occupancy/components/OccupancyStats'
import type { SpacePlacement } from '@/features/spaces/schemas'
import { sceneBounds } from '../geometry'
import { useLotLayout } from '../queries'
import {
  LABELS_AUTO_LIMIT,
  darkPalette,
  levelsOf,
  lightPalette,
  toSceneBays,
  type LevelFilter,
} from '../scene-model'
import { isWebGLAvailable } from '../webgl'
import { AutoArrangePanel } from './AutoArrangePanel'
import { LayoutLegend } from './LayoutLegend'
import { LayoutWarnings } from './LayoutWarnings'
import { LevelSwitcher } from './LevelSwitcher'
import { LotScene, type CameraMode } from './LotScene'
import { SpaceDetailsPanel } from './SpaceDetailsPanel'
import { SpaceList } from './SpaceList'
import { UnplacedTray } from './UnplacedTray'
import { ViewControls } from './ViewControls'

type Props = {
  lotId: string
}

type Draft = { spaceId: string; placement: SpacePlacement }

const EMPTY_OVERRIDES: ReadonlyMap<string, SpacePlacement> = new Map()

export function LotView({ lotId }: Props) {
  const { data, isLoading, isError } = useLotLayout(lotId)
  const { resolvedTheme } = useTheme()
  const palette = resolvedTheme === 'dark' ? darkPalette : lightPalette
  const [hasWebGL] = useState(isWebGLAvailable)

  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [hoveredId, setHoveredId] = useState<string | null>(null)
  const [levelFilter, setLevelFilter] = useState<LevelFilter>('all')
  const [cameraMode, setCameraMode] = useState<CameraMode>('perspective')
  const [resetToken, setResetToken] = useState(0)
  const [labelsPreference, setLabelsPreference] = useState<boolean | null>(null)
  const [draft, setDraft] = useState<Draft | null>(null)
  const [isArranging, setIsArranging] = useState(false)
  const [arrangePreview, setArrangePreview] = useState<ReadonlyMap<
    string,
    SpacePlacement
  > | null>(null)

  const spaces = useMemo(() => data?.spaces ?? [], [data])
  const layout = data?.lot.layout ?? null

  const overrides = useMemo(() => {
    if (arrangePreview) return arrangePreview
    if (draft) return new Map([[draft.spaceId, draft.placement]])
    return EMPTY_OVERRIDES
  }, [arrangePreview, draft])

  const bays = useMemo(
    () => toSceneBays(spaces, overrides),
    [spaces, overrides],
  )
  const savedPlacements = useMemo(
    () => spaces.flatMap((space) => (space.placement ? [space.placement] : [])),
    [spaces],
  )
  const bounds = useMemo(
    () => sceneBounds(layout, savedPlacements),
    [layout, savedPlacements],
  )
  const levels = useMemo(
    () => levelsOf(layout?.levelCount, bays),
    [layout, bays],
  )
  const placedSpaces = spaces.filter((space) => space.placement)
  const unplacedSpaces = spaces.filter((space) => !space.placement)
  const selectedSpace = spaces.find((space) => space.id === selectedId) ?? null
  const showLabels =
    labelsPreference ?? placedSpaces.length <= LABELS_AUTO_LIMIT

  const select = useCallback((id: string | null) => {
    setSelectedId(id)
    setDraft((current) => (current && current.spaceId !== id ? null : current))
  }, [])

  const handleDraftChange = useCallback(
    (placement: SpacePlacement | null) => {
      if (!selectedId) return
      setDraft(placement ? { spaceId: selectedId, placement } : null)
      if (
        placement &&
        levelFilter !== 'all' &&
        placement.level !== levelFilter
      ) {
        setLevelFilter(placement.level)
      }
    },
    [selectedId, levelFilter],
  )

  const handlePreviewChange = useCallback(
    (preview: ReadonlyMap<string, SpacePlacement> | null) =>
      setArrangePreview(preview),
    [],
  )

  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key !== 'Escape') return
      const target = event.target as HTMLElement | null
      if (target?.closest('[role="dialog"]')) return
      select(null)
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [select])

  if (isLoading) {
    return (
      <p className="p-6 text-sm text-muted-foreground">Loading lot layout…</p>
    )
  }

  if (isError || !data) {
    return (
      <p className="p-6 text-sm text-muted-foreground">
        Could not load this lot's layout.
      </p>
    )
  }

  return (
    <div className="flex flex-col gap-4 p-6">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold">{data.lot.name} — 3D view</h1>
          <p className="text-sm text-muted-foreground">
            {layout
              ? `Footprint ${layout.widthMeters} × ${layout.lengthMeters} m · ${layout.levelCount} level(s)`
              : 'No footprint set — ground sized from placed bays'}
            {data.lot.status === 'Archived' ? ' · Archived' : ''}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <RoleGate roles={['Operator', 'SystemAdmin']}>
            <Button
              size="sm"
              variant={isArranging ? 'default' : 'outline'}
              onClick={() => {
                setIsArranging((current) => !current)
                select(null)
              }}
            >
              <LayoutGrid />
              Auto-arrange
            </Button>
          </RoleGate>
          <Button size="sm" variant="outline" asChild>
            <Link to="/lots/$lotId" params={{ lotId }}>
              <Table2 />
              Table view
            </Link>
          </Button>
        </div>
      </header>

      <div className="flex flex-wrap items-center justify-between gap-2">
        <LevelSwitcher
          levels={levels}
          value={levelFilter}
          onChange={setLevelFilter}
        />
        <ViewControls
          cameraMode={cameraMode}
          showLabels={showLabels}
          onCameraModeChange={setCameraMode}
          onShowLabelsChange={setLabelsPreference}
          onReset={() => setResetToken((token) => token + 1)}
        />
      </div>

      <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_22rem]">
        <div className="relative h-[70vh] min-h-[420px] overflow-hidden rounded-lg border">
          {hasWebGL ? (
            <LotScene
              bays={bays}
              bounds={bounds}
              levels={levels}
              levelFilter={levelFilter}
              palette={palette}
              cameraMode={cameraMode}
              resetToken={resetToken}
              showLabels={showLabels}
              hoveredId={hoveredId}
              selectedId={selectedId}
              onHover={setHoveredId}
              onSelect={select}
            />
          ) : (
            <div className="flex h-full flex-col items-center justify-center gap-2 p-6 text-center">
              <p className="font-medium">3D view unavailable</p>
              <p className="text-sm text-muted-foreground">
                This browser or device does not support WebGL.
              </p>
              <Link
                to="/lots/$lotId"
                params={{ lotId }}
                className="text-sm underline"
              >
                Open the table view instead
              </Link>
            </div>
          )}
          <div className="pointer-events-none absolute bottom-3 left-3">
            <div className="pointer-events-auto">
              <LayoutLegend palette={palette} />
            </div>
          </div>
          <p className="pointer-events-none absolute top-3 left-3 rounded bg-background/80 px-2 py-1 text-xs text-muted-foreground">
            Drag to orbit · right-drag to pan · scroll to zoom · click a bay
          </p>
        </div>

        <aside className="space-y-5 lg:max-h-[70vh] lg:overflow-y-auto lg:pr-1">
          <LayoutWarnings
            bays={bays}
            layout={layout}
            unplacedCount={unplacedSpaces.length}
          />

          {isArranging ? (
            <AutoArrangePanel
              lotId={lotId}
              spaces={spaces}
              layout={layout}
              onPreviewChange={handlePreviewChange}
              onClose={() => setIsArranging(false)}
            />
          ) : selectedSpace ? (
            <SpaceDetailsPanel
              key={selectedSpace.id}
              lotId={lotId}
              space={selectedSpace}
              layout={layout}
              draftPlacement={
                draft?.spaceId === selectedSpace.id ? draft.placement : null
              }
              onDraftChange={handleDraftChange}
              onClose={() => select(null)}
            />
          ) : (
            <p className="text-sm text-muted-foreground">
              Select a bay in the scene or the list below to see its details.
            </p>
          )}

          {!isArranging ? (
            <>
              <SpaceList
                title="Placed spaces"
                spaces={placedSpaces}
                selectedId={selectedId}
                onSelect={select}
                searchable
                emptyText="No spaces are placed yet."
              />
              <UnplacedTray
                spaces={unplacedSpaces}
                selectedId={selectedId}
                onSelect={select}
              />
            </>
          ) : null}

          <div className="space-y-2 border-t pt-4">
            <p className="text-xs font-medium tracking-wide text-muted-foreground uppercase">
              Lot-level, counted at the gate — not per bay
            </p>
            <OccupancyStats lotId={lotId} compact />
          </div>
        </aside>
      </div>
    </div>
  )
}
