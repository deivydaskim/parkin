import { useState } from 'react'
import { Star, UserRound } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { ConfirmDialog } from '@/components/ConfirmDialog'
import type { ComboboxOption } from '@/components/EntityCombobox'
import { useHasRole } from '@/features/auth/permissions'
import { DriverCombobox } from '@/features/drivers/components/DriverCombobox'
import {
  useActiveReservation,
  useCancelReservation,
  useCreateReservation,
  useReassignReservation,
} from '../queries'

type Props = {
  lotId: string
  spaceId: string
  spaceLabel: string
  holderName?: string | null
  onDone?: () => void
}

export function ReservationPanel({
  lotId,
  spaceId,
  spaceLabel,
  holderName,
  onDone,
}: Props) {
  const canEdit = useHasRole('Operator', 'SystemAdmin')
  const [picked, setPicked] = useState<ComboboxOption | null>(null)
  const [confirmFree, setConfirmFree] = useState(false)
  const { data: active, isLoading } = useActiveReservation(spaceId, true)
  const createMutation = useCreateReservation(lotId, spaceId)
  const reassignMutation = useReassignReservation(lotId, spaceId)
  const cancelMutation = useCancelReservation(lotId, spaceId)

  const isSaving =
    createMutation.isPending ||
    reassignMutation.isPending ||
    cancelMutation.isPending
  const currentName = active ? (holderName ?? 'Assigned driver') : null
  const hasChange = !!picked && picked.id !== active?.driverId

  function finish() {
    setPicked(null)
    onDone?.()
  }

  function handleSave() {
    if (!picked) return
    if (active) {
      reassignMutation.mutate(
        { reservationId: active.id, driverId: picked.id },
        { onSuccess: finish },
      )
    } else {
      createMutation.mutate(picked.id, { onSuccess: finish })
    }
  }

  function handleFree() {
    if (!active) return
    cancelMutation.mutate(active.id, {
      onSuccess: () => {
        setConfirmFree(false)
        finish()
      },
    })
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3 rounded-lg border bg-muted/40 p-3">
        <span className="flex size-9 shrink-0 items-center justify-center rounded-full bg-warning/20 text-warning-foreground dark:text-warning">
          {currentName ? <UserRound className="size-4" /> : <Star className="size-4" />}
        </span>
        <div className="min-w-0">
          <p className="text-xs text-muted-foreground">Currently reserved for</p>
          {isLoading ? (
            <Skeleton className="mt-1 h-4 w-32" />
          ) : (
            <p className="truncate font-medium">{currentName ?? 'Nobody — space is free'}</p>
          )}
        </div>
      </div>

      {canEdit ? (
        <div className="space-y-3">
          <div className="space-y-2">
            <Label htmlFor={`reservation-${spaceId}`}>
              {active ? 'Give this space to' : 'Reserve for'}
            </Label>
            <DriverCombobox
              id={`reservation-${spaceId}`}
              value={picked?.id ?? active?.driverId ?? null}
              selectedLabel={picked?.label ?? currentName}
              onChange={setPicked}
              disabled={isLoading || isSaving}
            />
          </div>
          <div className="flex flex-wrap gap-2">
            <Button onClick={handleSave} disabled={!hasChange || isSaving}>
              {reassignMutation.isPending || createMutation.isPending
                ? 'Saving…'
                : active
                  ? 'Change holder'
                  : 'Assign'}
            </Button>
            {active ? (
              <Button
                variant="outline"
                onClick={() => setConfirmFree(true)}
                disabled={isSaving}
              >
                Free up space
              </Button>
            ) : null}
          </div>
        </div>
      ) : null}

      <ConfirmDialog
        open={confirmFree}
        onOpenChange={setConfirmFree}
        title={`Free up space ${spaceLabel}?`}
        description={`The reservation for ${currentName ?? 'this driver'} ends now. The space stays reserved-type but has no holder.`}
        confirmLabel="Free up space"
        pendingLabel="Freeing…"
        destructive
        isPending={cancelMutation.isPending}
        onConfirm={handleFree}
      />
    </div>
  )
}
