import { useState } from 'react'
import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { toCreateSpaceInput } from '../api'
import { useCreateSpace } from '../queries'
import type { SpaceFormInput } from '../schemas'
import { SpaceForm } from './SpaceForm'

type Props = {
  lotId: string
}

export function CreateSpaceDialog({ lotId }: Props) {
  const [open, setOpen] = useState(false)
  const createSpaceMutation = useCreateSpace(lotId)

  function handleOpenChange(next: boolean) {
    setOpen(next)
    if (!next) createSpaceMutation.reset()
  }

  function handleCreate(values: SpaceFormInput) {
    createSpaceMutation.mutate(toCreateSpaceInput(values), {
      onSuccess: () => handleOpenChange(false),
    })
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogTrigger asChild>
        <Button>
          <Plus />
          New space
        </Button>
      </DialogTrigger>
      <DialogContent className="max-h-[90svh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Create parking space</DialogTitle>
          <DialogDescription>
            General spaces add to lot capacity; reserved spaces are held for
            one driver.
          </DialogDescription>
        </DialogHeader>
        <SpaceForm
          onSubmit={handleCreate}
          isSubmitting={createSpaceMutation.isPending}
          error={createSpaceMutation.error}
          submitLabel="Create space"
        />
      </DialogContent>
    </Dialog>
  )
}
