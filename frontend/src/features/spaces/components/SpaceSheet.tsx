import { useState } from 'react'
import { Power, PowerOff } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Separator } from '@/components/ui/separator'
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { ConfirmDialog } from '@/components/ConfirmDialog'
import { StatusBadge } from '@/components/StatusBadge'
import { RoleGate } from '@/features/auth/components/RoleGate'
import { ReservationPanel } from '@/features/reservations/components/ReservationPanel'
import { toUpdateSpaceInput } from '../api'
import { spaceToFormValues } from '../form-values'
import {
  useDeactivateSpace,
  useReactivateSpace,
  useUpdateSpace,
} from '../queries'
import {
  SpaceStatus,
  SpaceType,
  type Space,
  type SpaceFormInput,
} from '../schemas'
import { SpaceForm } from './SpaceForm'

type Props = {
  lotId: string
  space: Space | null
  onOpenChange: (open: boolean) => void
}

export function SpaceSheet({ lotId, space, onOpenChange }: Props) {
  return (
    <Sheet open={!!space} onOpenChange={onOpenChange}>
      <SheetContent className="w-full gap-0 overflow-y-auto sm:max-w-md">
        {space ? <SpaceSheetBody key={space.id} lotId={lotId} space={space} /> : null}
      </SheetContent>
    </Sheet>
  )
}

function SpaceSheetBody({ lotId, space }: { lotId: string; space: Space }) {
  const [confirmDeactivate, setConfirmDeactivate] = useState(false)
  const updateMutation = useUpdateSpace(space.id, lotId)
  const deactivateMutation = useDeactivateSpace(space.id, lotId)
  const reactivateMutation = useReactivateSpace(space.id, lotId)
  const isActive = space.status === SpaceStatus.Active

  function handleUpdate(values: SpaceFormInput) {
    updateMutation.mutate(toUpdateSpaceInput(values, space.placement !== null))
  }

  return (
    <>
      <SheetHeader className="border-b">
        <SheetTitle className="font-mono text-xl">{space.label}</SheetTitle>
        <SheetDescription className="flex flex-wrap items-center gap-1.5">
          <StatusBadge status={space.type} />
          <StatusBadge status={space.status} />
          {space.zone ? <span className="text-xs">Zone {space.zone}</span> : null}
        </SheetDescription>
      </SheetHeader>

      <div className="space-y-6 p-4">
        {space.type === SpaceType.Reserved && isActive ? (
          <section className="space-y-3">
            <h3 className="text-sm font-semibold">Reservation</h3>
            <ReservationPanel
              lotId={lotId}
              spaceId={space.id}
              spaceLabel={space.label}
              holderName={space.reservedDriverName}
            />
          </section>
        ) : null}

        <RoleGate
          roles={['Operator', 'SystemAdmin']}
          fallback={
            <p className="text-sm text-muted-foreground">
              You can view this space but not change it.
            </p>
          }
        >
          {space.type === SpaceType.Reserved && isActive ? <Separator /> : null}

          <section className="space-y-3">
            <h3 className="text-sm font-semibold">Details</h3>
            <SpaceForm
              defaultValues={spaceToFormValues(space)}
              onSubmit={handleUpdate}
              isSubmitting={updateMutation.isPending}
              error={updateMutation.error}
              submitLabel="Save changes"
            />
          </section>

          <Separator />

          <section className="space-y-3">
            <h3 className="text-sm font-semibold">Availability</h3>
            {isActive ? (
              <div className="flex items-center justify-between gap-3 rounded-lg border border-destructive/30 p-3">
                <p className="text-sm text-muted-foreground">
                  Take this space out of service. It stops counting towards
                  capacity.
                </p>
                <Button
                  variant="outline"
                  className="shrink-0 border-destructive/40 text-destructive hover:bg-destructive/10 hover:text-destructive"
                  onClick={() => setConfirmDeactivate(true)}
                >
                  <PowerOff />
                  Deactivate
                </Button>
              </div>
            ) : (
              <div className="flex items-center justify-between gap-3 rounded-lg border p-3">
                <p className="text-sm text-muted-foreground">
                  This space is out of service.
                </p>
                <Button
                  className="shrink-0"
                  disabled={reactivateMutation.isPending}
                  onClick={() => reactivateMutation.mutate()}
                >
                  <Power />
                  {reactivateMutation.isPending ? 'Reactivating…' : 'Reactivate'}
                </Button>
              </div>
            )}
          </section>
        </RoleGate>
      </div>

      <ConfirmDialog
        open={confirmDeactivate}
        onOpenChange={setConfirmDeactivate}
        title={`Deactivate space ${space.label}?`}
        description="It stops counting towards lot capacity and can't be used until reactivated. A space with an active reservation can't be deactivated."
        confirmLabel="Deactivate"
        pendingLabel="Deactivating…"
        destructive
        isPending={deactivateMutation.isPending}
        onConfirm={() =>
          deactivateMutation.mutate(undefined, {
            onSuccess: () => setConfirmDeactivate(false),
          })
        }
      />
    </>
  )
}
