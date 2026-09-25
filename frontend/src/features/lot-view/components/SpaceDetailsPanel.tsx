import { useState } from 'react'
import { X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { RoleGate } from '@/features/auth/components/RoleGate'
import type { LotLayout } from '@/features/lots/schemas'
import { ReservationDialog } from '@/features/reservations/components/ReservationDialog'
import { toUpdateSpaceInput } from '@/features/spaces/api'
import { SpaceForm } from '@/features/spaces/components/SpaceForm'
import { spaceToFormValues } from '@/features/spaces/form-values'
import { useUpdateSpace } from '@/features/spaces/queries'
import {
  DEFAULT_BAY_LENGTH,
  DEFAULT_BAY_WIDTH,
  type SpaceFormInput,
  type SpacePlacement,
} from '@/features/spaces/schemas'
import { roundTo } from '../geometry'
import type { LayoutSpace } from '../schemas'
import { levelName } from '../scene-model'
import { PlacementNudger } from './PlacementNudger'

type Props = {
  lotId: string
  space: LayoutSpace
  layout: LotLayout | null
  draftPlacement: SpacePlacement | null
  onDraftChange: (placement: SpacePlacement | null) => void
  onClose: () => void
}

function initialPlacementFor(layout: LotLayout | null): SpacePlacement {
  return {
    x: layout ? roundTo(layout.widthMeters / 2, 2) : DEFAULT_BAY_WIDTH,
    y: layout ? roundTo(layout.lengthMeters / 2, 2) : DEFAULT_BAY_LENGTH / 2,
    rotationDegrees: 0,
    level: 0,
    width: DEFAULT_BAY_WIDTH,
    length: DEFAULT_BAY_LENGTH,
  }
}

function Detail({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex justify-between gap-3 py-1 text-sm">
      <dt className="text-muted-foreground">{label}</dt>
      <dd className="text-right font-medium">{value}</dd>
    </div>
  )
}

export function SpaceDetailsPanel({
  lotId,
  space,
  layout,
  draftPlacement,
  onDraftChange,
  onClose,
}: Props) {
  const [isEditOpen, setIsEditOpen] = useState(false)
  const updateSpaceMutation = useUpdateSpace(space.id, lotId)
  const effectivePlacement = draftPlacement ?? space.placement
  const maxLevel = layout ? layout.levelCount - 1 : null

  function handleEdit(values: SpaceFormInput) {
    updateSpaceMutation.mutate(
      toUpdateSpaceInput(values, space.placement !== null),
      {
        onSuccess: () => {
          setIsEditOpen(false)
          onDraftChange(null)
          updateSpaceMutation.reset()
        },
      },
    )
  }

  function handleSavePlacement() {
    if (!draftPlacement) return
    updateSpaceMutation.mutate(
      { placement: draftPlacement },
      { onSuccess: () => onDraftChange(null) },
    )
  }

  return (
    <section aria-labelledby="space-details-heading" className="space-y-4">
      <div className="flex items-start justify-between gap-2">
        <div>
          <h2 id="space-details-heading" className="text-lg font-semibold">
            Space {space.label}
          </h2>
          <p className="text-xs text-muted-foreground">
            Press Esc to clear the selection.
          </p>
        </div>
        <Button
          size="icon"
          variant="ghost"
          aria-label="Clear selection"
          onClick={onClose}
        >
          <X />
        </Button>
      </div>

      <dl className="divide-y rounded-md border px-3">
        <Detail label="Type" value={space.type} />
        <Detail label="Status" value={space.status} />
        <Detail label="Zone" value={space.zone ?? '—'} />
        <Detail
          label="Assignee"
          value={
            space.type === 'Reserved'
              ? (space.reservation?.driverName ?? 'Unassigned')
              : 'Not reservable'
          }
        />
        <Detail
          label="Position"
          value={
            space.placement
              ? `${space.placement.x}, ${space.placement.y} m · ${space.placement.rotationDegrees}°`
              : 'Unplaced'
          }
        />
        {space.placement ? (
          <Detail
            label="Level · size"
            value={`${levelName(space.placement.level)} · ${space.placement.width} × ${space.placement.length} m`}
          />
        ) : null}
      </dl>

      <div className="flex flex-wrap gap-2">
        {space.type === 'Reserved' ? (
          <ReservationDialog
            lotId={lotId}
            spaceId={space.id}
            spaceLabel={space.label}
            holderName={space.reservation?.driverName}
          />
        ) : null}

        <RoleGate roles={['Operator', 'SystemAdmin']}>
          <Dialog open={isEditOpen} onOpenChange={setIsEditOpen}>
            <DialogTrigger asChild>
              <Button variant="outline" size="sm">
                Edit space
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Edit parking space</DialogTitle>
              </DialogHeader>
              <SpaceForm
                defaultValues={spaceToFormValues({
                  ...space,
                  placement: effectivePlacement,
                })}
                onSubmit={handleEdit}
                isSubmitting={updateSpaceMutation.isPending}
                error={updateSpaceMutation.error}
                submitLabel="Save changes"
              />
            </DialogContent>
          </Dialog>

          {!effectivePlacement ? (
            <Button
              size="sm"
              onClick={() => onDraftChange(initialPlacementFor(layout))}
            >
              Place on map
            </Button>
          ) : null}
        </RoleGate>
      </div>

      {effectivePlacement ? (
        <RoleGate roles={['Operator', 'SystemAdmin']}>
          <PlacementNudger
            placement={effectivePlacement}
            maxLevel={maxLevel}
            isDirty={draftPlacement !== null}
            isSaving={updateSpaceMutation.isPending}
            onChange={onDraftChange}
            onSave={handleSavePlacement}
            onDiscard={() => onDraftChange(null)}
          />
        </RoleGate>
      ) : null}
    </section>
  )
}
