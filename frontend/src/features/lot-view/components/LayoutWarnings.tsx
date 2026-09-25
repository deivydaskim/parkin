import { useMemo } from 'react'
import { TriangleAlert } from 'lucide-react'
import type { LotLayout } from '@/features/lots/schemas'
import { findOverlaps, isOutsideFootprint } from '../geometry'
import type { SceneBay } from '../scene-model'

type Props = {
  bays: SceneBay[]
  layout: LotLayout | null
  unplacedCount: number
}

export function LayoutWarnings({ bays, layout, unplacedCount }: Props) {
  const { overlapLabels, outsideLabels } = useMemo(() => {
    const labelById = new Map(bays.map((bay) => [bay.id, bay.label]))
    const overlaps = findOverlaps(
      bays.map((bay) => ({ id: bay.id, placement: bay.placement })),
    )
    return {
      overlapLabels: overlaps.map(
        ([a, b]) => `${labelById.get(a)} ↔ ${labelById.get(b)}`,
      ),
      outsideLabels: layout
        ? bays
            .filter((bay) => isOutsideFootprint(bay.placement, layout))
            .map((bay) => bay.label)
        : [],
    }
  }, [bays, layout])

  const hasWarnings = overlapLabels.length > 0 || outsideLabels.length > 0

  return (
    <div role="status" aria-live="polite" className="space-y-1 text-xs">
      <p className="text-muted-foreground">
        {bays.length} placed · {unplacedCount} unplaced
      </p>
      {hasWarnings ? (
        <div className="rounded-md border border-amber-500/40 bg-amber-500/10 p-2 text-amber-900 dark:text-amber-200">
          {overlapLabels.length > 0 ? (
            <p className="flex items-start gap-1.5">
              <TriangleAlert className="mt-0.5 size-3.5 shrink-0" />
              <span>
                {overlapLabels.length} overlapping pair(s):{' '}
                {overlapLabels.slice(0, 5).join(', ')}
                {overlapLabels.length > 5 ? '…' : ''}
              </span>
            </p>
          ) : null}
          {outsideLabels.length > 0 ? (
            <p className="flex items-start gap-1.5">
              <TriangleAlert className="mt-0.5 size-3.5 shrink-0" />
              <span>
                {outsideLabels.length} bay(s) outside the footprint:{' '}
                {outsideLabels.slice(0, 8).join(', ')}
                {outsideLabels.length > 8 ? '…' : ''}
              </span>
            </p>
          ) : null}
        </div>
      ) : null}
    </div>
  )
}
