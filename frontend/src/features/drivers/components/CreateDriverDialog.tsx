import { useNavigate } from '@tanstack/react-router'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { useCreateDriver } from '../queries'
import type { DriverFormInput } from '../schemas'
import { DriverForm } from './DriverForm'

type Props = {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function CreateDriverDialog({ open, onOpenChange }: Props) {
  const navigate = useNavigate()
  const createDriverMutation = useCreateDriver()

  function handleOpenChange(next: boolean) {
    onOpenChange(next)
    if (!next) createDriverMutation.reset()
  }

  function handleCreate(values: DriverFormInput) {
    createDriverMutation.mutate(values, {
      onSuccess: (driver) => {
        handleOpenChange(false)
        navigate({
          to: '/drivers/$driverId',
          params: { driverId: driver.id },
        })
      },
    })
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>New driver</DialogTitle>
          <DialogDescription>
            Add their plates and lot access on the next screen.
          </DialogDescription>
        </DialogHeader>
        <DriverForm
          onSubmit={handleCreate}
          isSubmitting={createDriverMutation.isPending}
          error={createDriverMutation.error}
          submitLabel="Create driver"
        />
      </DialogContent>
    </Dialog>
  )
}
