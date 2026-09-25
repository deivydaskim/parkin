import { useEffect, useId, useMemo, useState } from 'react'
import { Plus, Trash2, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Switch } from '@/components/ui/switch'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import type { LotLayout } from '@/features/lots/schemas'
import type { SpacePlacement } from '@/features/spaces/schemas'
import {
  BAY_ANGLES,
  defaultRow,
  generateLayout,
  type BayAngle,
  type RowSpec,
} from '../auto-arrange'
import { boundsOf } from '../geometry'
import { useApplyLotLayout } from '../queries'
import type { ApplyLayoutInput, LayoutSpace } from '../schemas'

type Props = {
  lotId: string
  spaces: LayoutSpace[]
  layout: LotLayout | null
  onPreviewChange: (preview: ReadonlyMap<string, SpacePlacement> | null) => void
  onClose: () => void
}

const FOOTPRINT_MARGIN = 3

function twoFacingRows(bayCount: number): RowSpec[] {
  const perRow = Math.max(1, Math.ceil(bayCount / 2))
  return [
    defaultRow({ start: { x: 4.25, y: 3.5 }, bayCount: perRow }),
    defaultRow({ bayCount: perRow, flip: true }),
  ]
}

function fittedLayout(
  current: LotLayout | null,
  placements: SpacePlacement[],
): ApplyLayoutInput['layout'] {
  const bounds = boundsOf(placements)
  const levelCount = Math.max(
    current?.levelCount ?? 1,
    ...placements.map((p) => p.level + 1),
  )
  return {
    widthMeters: fitDimension(current?.widthMeters, bounds?.maxX ?? 0),
    lengthMeters: fitDimension(current?.lengthMeters, bounds?.maxY ?? 0),
    levelCount,
  }
}

function fitDimension(current: number | undefined, needed: number) {
  if (current !== undefined && current >= needed) return current
  return Math.ceil(needed + FOOTPRINT_MARGIN)
}

export function AutoArrangePanel({
  lotId,
  spaces,
  layout,
  onPreviewChange,
  onClose,
}: Props) {
  const [onlyUnplaced, setOnlyUnplaced] = useState(() =>
    spaces.some((s) => !s.placement),
  )
  const candidateCount = onlyUnplaced
    ? spaces.filter((s) => !s.placement).length
    : spaces.length
  const [rows, setRows] = useState<RowSpec[]>(() =>
    twoFacingRows(candidateCount),
  )
  const [fitFootprint, setFitFootprint] = useState(true)
  const applyMutation = useApplyLotLayout(lotId)

  const result = useMemo(
    () =>
      generateLayout(
        rows,
        spaces.map((space) => ({
          id: space.id,
          label: space.label,
          isPlaced: space.placement !== null,
        })),
        { onlyUnplaced },
      ),
    [rows, spaces, onlyUnplaced],
  )

  const preview = useMemo(
    () =>
      new Map(
        result.changes.map((change) => [change.spaceId, change.placement!]),
      ),
    [result],
  )

  useEffect(() => {
    onPreviewChange(preview)
  }, [preview, onPreviewChange])

  useEffect(() => () => onPreviewChange(null), [onPreviewChange])

  const hasNegative = result.changes.some(
    (change) => change.placement!.x < 0 || change.placement!.y < 0,
  )

  function updateRow(index: number, patch: Partial<RowSpec>) {
    setRows((current) =>
      current.map((row, i) => (i === index ? { ...row, ...patch } : row)),
    )
  }

  function handleApply() {
    const changedIds = new Set(result.changes.map((c) => c.spaceId))
    const finalPlacements = [
      ...spaces
        .filter((s) => s.placement && !changedIds.has(s.id))
        .map((s) => s.placement!),
      ...result.changes.map((c) => c.placement!),
    ]
    const input: ApplyLayoutInput = {
      layout: fitFootprint ? fittedLayout(layout, finalPlacements) : undefined,
      spaces: result.changes,
    }
    applyMutation.mutate(input, { onSuccess: onClose })
  }

  return (
    <section aria-labelledby="auto-arrange-heading" className="space-y-4">
      <div className="flex items-start justify-between gap-2">
        <div>
          <h2 id="auto-arrange-heading" className="text-lg font-semibold">
            Auto-arrange
          </h2>
          <p className="text-xs text-muted-foreground">
            Define rows; the preview updates live in the scene. Nothing is saved
            until you apply.
          </p>
        </div>
        <Button
          size="icon"
          variant="ghost"
          aria-label="Close auto-arrange"
          onClick={onClose}
        >
          <X />
        </Button>
      </div>

      <div className="space-y-2 rounded-md border p-3">
        <ToggleRow
          id="only-unplaced"
          label="Only unplaced spaces"
          checked={onlyUnplaced}
          onChange={setOnlyUnplaced}
        />
        <ToggleRow
          id="fit-footprint"
          label="Grow footprint to fit"
          checked={fitFootprint}
          onChange={setFitFootprint}
        />
        <div className="flex flex-wrap gap-2 pt-1">
          <Button
            size="sm"
            variant="outline"
            onClick={() => setRows(twoFacingRows(candidateCount))}
          >
            Preset: two facing rows
          </Button>
        </div>
      </div>

      <ol className="space-y-3">
        {rows.map((row, index) => (
          <li key={index} className="space-y-2 rounded-md border p-3">
            <div className="flex items-center justify-between">
              <p className="text-sm font-medium">
                Row {index + 1}{' '}
                <span className="text-xs font-normal text-muted-foreground">
                  {result.rows[index]?.assigned ?? 0}/{row.bayCount} bays filled
                </span>
              </p>
              <Button
                size="icon"
                variant="ghost"
                aria-label={`Remove row ${index + 1}`}
                disabled={rows.length === 1}
                onClick={() =>
                  setRows((current) => current.filter((_, i) => i !== index))
                }
              >
                <Trash2 />
              </Button>
            </div>

            {index > 0 ? (
              <ToggleRow
                id={`follow-${index}`}
                label="Follow previous row"
                checked={row.start === null}
                onChange={(follow) =>
                  updateRow(index, {
                    start: follow
                      ? null
                      : (result.rows[index]?.start ?? { x: 0, y: 0 }),
                  })
                }
              />
            ) : null}

            <div className="grid grid-cols-3 gap-2">
              {row.start !== null || index === 0 ? (
                <>
                  <NumberInput
                    label="Start X"
                    value={row.start?.x ?? 0}
                    onChange={(x) =>
                      updateRow(index, { start: { x, y: row.start?.y ?? 0 } })
                    }
                  />
                  <NumberInput
                    label="Start Y"
                    value={row.start?.y ?? 0}
                    onChange={(y) =>
                      updateRow(index, { start: { x: row.start?.x ?? 0, y } })
                    }
                  />
                </>
              ) : null}
              <NumberInput
                label="Direction °"
                value={row.directionDegrees}
                step={15}
                onChange={(directionDegrees) =>
                  updateRow(index, { directionDegrees })
                }
              />
              <NumberInput
                label="Bays"
                value={row.bayCount}
                step={1}
                min={1}
                onChange={(bayCount) =>
                  updateRow(index, {
                    bayCount: Math.max(1, Math.round(bayCount)),
                  })
                }
              />
              <NumberInput
                label="Bay width"
                value={row.bayWidth}
                step={0.1}
                min={0.5}
                onChange={(bayWidth) =>
                  updateRow(index, { bayWidth: Math.max(0.5, bayWidth) })
                }
              />
              <NumberInput
                label="Bay length"
                value={row.bayLength}
                step={0.1}
                min={0.5}
                onChange={(bayLength) =>
                  updateRow(index, { bayLength: Math.max(0.5, bayLength) })
                }
              />
              <div className="space-y-1">
                <Label className="text-xs">Bay angle</Label>
                <Select
                  value={String(row.bayAngle)}
                  onValueChange={(value) =>
                    updateRow(index, { bayAngle: Number(value) as BayAngle })
                  }
                >
                  <SelectTrigger size="sm" className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {BAY_ANGLES.map((angle) => (
                      <SelectItem key={angle} value={String(angle)}>
                        {angle}°
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <NumberInput
                label="Aisle after"
                value={row.aisleAfter}
                step={0.5}
                min={0}
                onChange={(aisleAfter) =>
                  updateRow(index, { aisleAfter: Math.max(0, aisleAfter) })
                }
              />
              <NumberInput
                label="Level"
                value={row.level}
                step={1}
                min={0}
                onChange={(level) =>
                  updateRow(index, { level: Math.max(0, Math.round(level)) })
                }
              />
            </div>

            <ToggleRow
              id={`flip-${index}`}
              label="Face the opposite way"
              checked={row.flip}
              onChange={(flip) => updateRow(index, { flip })}
            />

            <div className="grid grid-cols-2 gap-2">
              <div className="space-y-1">
                <Label htmlFor={`zone-${index}`} className="text-xs">
                  Zone
                </Label>
                <Input
                  id={`zone-${index}`}
                  className="h-8"
                  value={row.zone}
                  placeholder="Keep existing"
                  onChange={(event) =>
                    updateRow(index, { zone: event.target.value })
                  }
                />
              </div>
              <div className="space-y-1">
                <Label htmlFor={`filter-${index}`} className="text-xs">
                  Label filter
                </Label>
                <Input
                  id={`filter-${index}`}
                  className="h-8"
                  placeholder="Next in label order"
                  value={
                    row.source.mode === 'filter' ? row.source.labelFilter : ''
                  }
                  onChange={(event) =>
                    updateRow(index, {
                      source: event.target.value
                        ? { mode: 'filter', labelFilter: event.target.value }
                        : { mode: 'labelOrder' },
                    })
                  }
                />
              </div>
            </div>
          </li>
        ))}
      </ol>

      <Button
        size="sm"
        variant="outline"
        onClick={() =>
          setRows((current) => [
            ...current,
            defaultRow({ flip: !current.at(-1)?.flip }),
          ])
        }
      >
        <Plus />
        Add row
      </Button>

      <div className="space-y-2 border-t pt-3">
        <p className="text-sm">
          {result.changes.length} of {candidateCount} candidate space(s) will be
          placed.
        </p>
        {hasNegative ? (
          <p role="alert" className="text-xs text-destructive">
            Some bays fall below 0 m — move the start point so every bay has
            non-negative X and Y.
          </p>
        ) : null}
        <div className="flex gap-2">
          <Button
            disabled={
              result.changes.length === 0 ||
              hasNegative ||
              applyMutation.isPending
            }
            onClick={handleApply}
          >
            {applyMutation.isPending ? 'Applying…' : 'Apply layout'}
          </Button>
          <Button variant="ghost" onClick={onClose}>
            Cancel
          </Button>
        </div>
      </div>
    </section>
  )
}

type ToggleRowProps = {
  id: string
  label: string
  checked: boolean
  onChange: (checked: boolean) => void
}

function ToggleRow({ id, label, checked, onChange }: ToggleRowProps) {
  return (
    <div className="flex items-center justify-between gap-2">
      <Label htmlFor={id} className="text-sm font-normal">
        {label}
      </Label>
      <Switch id={id} checked={checked} onCheckedChange={onChange} />
    </div>
  )
}

type NumberInputProps = {
  label: string
  value: number
  onChange: (value: number) => void
  step?: number
  min?: number
}

function NumberInput({
  label,
  value,
  onChange,
  step = 0.5,
  min,
}: NumberInputProps) {
  const id = useId()
  const [draft, setDraft] = useState(String(value))
  const [lastValue, setLastValue] = useState(value)

  if (value !== lastValue) {
    setLastValue(value)
    setDraft(String(value))
  }

  return (
    <div className="space-y-1">
      <Label htmlFor={id} className="text-xs">
        {label}
      </Label>
      <Input
        id={id}
        type="number"
        inputMode="decimal"
        className="h-8"
        step={step}
        min={min}
        value={draft}
        onChange={(event) => {
          setDraft(event.target.value)
          const next = event.target.valueAsNumber
          if (Number.isFinite(next)) {
            setLastValue(next)
            onChange(next)
          }
        }}
      />
    </div>
  )
}
