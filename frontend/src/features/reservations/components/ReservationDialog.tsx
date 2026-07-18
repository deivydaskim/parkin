import { useState } from 'react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { RoleGate } from '@/features/auth/components/RoleGate'
import { useDrivers } from '@/features/drivers/queries'
import {
  useActiveReservation,
  useCancelReservation,
  useCreateReservation,
} from '../queries'

const NONE_VALUE = '__none__'

type Props = {
  lotId: string
  spaceId: string
  spaceLabel: string
}

export function ReservationDialog({ lotId, spaceId, spaceLabel }: Props) {
  const [open, setOpen] = useState(false)
  // Only tracks an explicit choice the user made in this dialog session — until then,
  // the Select mirrors whatever the active-reservation fetch resolves to.
  const [driverIdOverride, setDriverIdOverride] = useState<string>()

  const { data: activeReservation, isLoading } = useActiveReservation(spaceId, open)
  const { data: driversData } = useDrivers({ perPage: 100, status: 'Active' })
  const createReservationMutation = useCreateReservation(lotId, spaceId)
  const cancelReservationMutation = useCancelReservation(lotId, spaceId)

  const drivers = driversData?.items ?? []
  const driverNameById = new Map(
    drivers.map((driver) => [driver.id, driver.name]),
  )
  const isSubmitting =
    createReservationMutation.isPending || cancelReservationMutation.isPending
  const selectedDriverId =
    driverIdOverride ?? activeReservation?.driverId ?? NONE_VALUE

  function handleOpenChange(next: boolean) {
    setOpen(next)
    if (!next) {
      setDriverIdOverride(undefined)
    }
  }

  function handleSave() {
    const nextDriverId =
      selectedDriverId === NONE_VALUE ? null : selectedDriverId
    const currentDriverId = activeReservation?.driverId ?? null
    if (nextDriverId === currentDriverId) {
      handleOpenChange(false)
      return
    }

    if (activeReservation) {
      cancelReservationMutation.mutate(activeReservation.id, {
        onSuccess: () => {
          if (nextDriverId) {
            createReservationMutation.mutate(nextDriverId, {
              onSuccess: () => handleOpenChange(false),
            })
          } else {
            handleOpenChange(false)
          }
        },
      })
    } else if (nextDriverId) {
      createReservationMutation.mutate(nextDriverId, {
        onSuccess: () => handleOpenChange(false),
      })
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogTrigger asChild>
        <Button variant="outline" size="sm">
          Reservation
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Reservation — {spaceLabel}</DialogTitle>
        </DialogHeader>

        <p className="text-sm text-muted-foreground">
          Currently reserved by:{' '}
          {isLoading
            ? '…'
            : activeReservation
              ? (driverNameById.get(activeReservation.driverId) ??
                activeReservation.driverId)
              : 'nobody'}
        </p>

        <RoleGate roles={['Operator', 'SystemAdmin']}>
          <Select
            value={selectedDriverId}
            onValueChange={setDriverIdOverride}
            disabled={isLoading}
          >
            <SelectTrigger>
              <SelectValue placeholder="Select a driver" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={NONE_VALUE}>— None —</SelectItem>
              {drivers.map((driver) => (
                <SelectItem key={driver.id} value={driver.id}>
                  {driver.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <DialogFooter>
            <Button disabled={isSubmitting || isLoading} onClick={handleSave}>
              {isSubmitting ? 'Saving…' : 'Save'}
            </Button>
          </DialogFooter>
        </RoleGate>
      </DialogContent>
    </Dialog>
  )
}
