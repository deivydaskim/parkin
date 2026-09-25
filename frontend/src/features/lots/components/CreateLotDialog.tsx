import { useNavigate } from '@tanstack/react-router'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { useCreateLot } from '../queries'
import type { LotFormInput } from '../schemas'
import { LotForm } from './LotForm'

type Props = {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function CreateLotDialog({ open, onOpenChange }: Props) {
  const navigate = useNavigate()
  const createLotMutation = useCreateLot()

  function handleOpenChange(next: boolean) {
    onOpenChange(next)
    if (!next) createLotMutation.reset()
  }

  function handleCreate(values: LotFormInput) {
    createLotMutation.mutate(values, {
      onSuccess: (lot) => {
        handleOpenChange(false)
        navigate({ to: '/lots/$lotId', params: { lotId: lot.id } })
      },
    })
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="max-h-[90svh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Create parking lot</DialogTitle>
          <DialogDescription>
            You can add spaces and access rules once the lot exists.
          </DialogDescription>
        </DialogHeader>
        <LotForm
          onSubmit={handleCreate}
          isSubmitting={createLotMutation.isPending}
          error={createLotMutation.error}
          submitLabel="Create lot"
        />
      </DialogContent>
    </Dialog>
  )
}
