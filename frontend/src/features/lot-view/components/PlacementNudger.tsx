import { useState } from 'react'
import {
  ArrowDown,
  ArrowLeft,
  ArrowRight,
  ArrowUp,
  ChevronDown,
  ChevronUp,
  RotateCcw,
  RotateCw,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import type { SpacePlacement } from '@/features/spaces/schemas'
import { normalizeDegrees, roundTo } from '../geometry'

type Props = {
  placement: SpacePlacement
  maxLevel: number | null
  isDirty: boolean
  isSaving: boolean
  onChange: (placement: SpacePlacement) => void
  onSave: () => void
  onDiscard: () => void
}

const STEPS = [0.1, 0.5, 1, 2.5, 5]
const ROTATION_STEP = 15

export function PlacementNudger({
  placement,
  maxLevel,
  isDirty,
  isSaving,
  onChange,
  onSave,
  onDiscard,
}: Props) {
  const [step, setStep] = useState(0.5)

  function move(dx: number, dy: number) {
    onChange({
      ...placement,
      x: Math.max(0, roundTo(placement.x + dx * step, 2)),
      y: Math.max(0, roundTo(placement.y + dy * step, 2)),
    })
  }

  function rotate(delta: number) {
    onChange({
      ...placement,
      rotationDegrees: normalizeDegrees(placement.rotationDegrees + delta),
    })
  }

  function changeLevel(delta: number) {
    const next = placement.level + delta
    if (next < 0 || (maxLevel !== null && next > maxLevel)) return
    onChange({ ...placement, level: next })
  }

  return (
    <div className="space-y-3 rounded-md border p-3">
      <div className="flex items-center justify-between gap-2">
        <p className="text-sm font-medium">Adjust placement</p>
        <div className="flex items-center gap-2">
          <Label htmlFor="nudge-step" className="text-xs text-muted-foreground">
            Step
          </Label>
          <Select
            value={String(step)}
            onValueChange={(value) => setStep(Number(value))}
          >
            <SelectTrigger id="nudge-step" size="sm" className="w-24">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {STEPS.map((value) => (
                <SelectItem key={value} value={String(value)}>
                  {value} m
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-1.5">
        <span />
        <Button
          size="sm"
          variant="outline"
          aria-label={`Move +Y by ${step} m`}
          onClick={() => move(0, 1)}
        >
          <ArrowUp />
        </Button>
        <span />
        <Button
          size="sm"
          variant="outline"
          aria-label={`Move −X by ${step} m`}
          onClick={() => move(-1, 0)}
        >
          <ArrowLeft />
        </Button>
        <Button
          size="sm"
          variant="outline"
          aria-label={`Move −Y by ${step} m`}
          onClick={() => move(0, -1)}
        >
          <ArrowDown />
        </Button>
        <Button
          size="sm"
          variant="outline"
          aria-label={`Move +X by ${step} m`}
          onClick={() => move(1, 0)}
        >
          <ArrowRight />
        </Button>
      </div>

      <div className="flex flex-wrap gap-1.5">
        <Button
          size="sm"
          variant="outline"
          onClick={() => rotate(ROTATION_STEP)}
        >
          <RotateCcw />+{ROTATION_STEP}°
        </Button>
        <Button
          size="sm"
          variant="outline"
          onClick={() => rotate(-ROTATION_STEP)}
        >
          <RotateCw />−{ROTATION_STEP}°
        </Button>
        <Button
          size="sm"
          variant="outline"
          disabled={maxLevel !== null && placement.level >= maxLevel}
          onClick={() => changeLevel(1)}
        >
          <ChevronUp />
          Level up
        </Button>
        <Button
          size="sm"
          variant="outline"
          disabled={placement.level <= 0}
          onClick={() => changeLevel(-1)}
        >
          <ChevronDown />
          Level down
        </Button>
      </div>

      <p className="text-xs tabular-nums text-muted-foreground">
        x {placement.x} m · y {placement.y} m · {placement.rotationDegrees}° ·
        level {placement.level}
      </p>

      <div className="flex gap-2">
        <Button size="sm" disabled={!isDirty || isSaving} onClick={onSave}>
          {isSaving ? 'Saving…' : 'Save placement'}
        </Button>
        <Button
          size="sm"
          variant="ghost"
          disabled={!isDirty || isSaving}
          onClick={onDiscard}
        >
          Discard
        </Button>
      </div>
    </div>
  )
}
