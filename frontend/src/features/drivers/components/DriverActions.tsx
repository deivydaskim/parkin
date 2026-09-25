import { useState } from 'react'
import { Archive, ArchiveRestore, MoreHorizontal, Pencil } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { ConfirmDialog } from '@/components/ConfirmDialog'
import {
  useArchiveDriver,
  useRestoreDriver,
  useUpdateDriver,
} from '../queries'
import { DriverStatus, type Driver, type DriverFormInput } from '../schemas'
import { DriverForm } from './DriverForm'

type Props = {
  driver: Driver
}

export function DriverActions({ driver }: Props) {
  const [isEditOpen, setIsEditOpen] = useState(false)
  const [confirm, setConfirm] = useState<'archive' | 'restore' | null>(null)
  const updateMutation = useUpdateDriver(driver.id)
  const archiveMutation = useArchiveDriver(driver.id)
  const restoreMutation = useRestoreDriver(driver.id)
  const isArchived = driver.status === DriverStatus.Archived
  const isActive = driver.status === DriverStatus.Active
  const isArchive = confirm === 'archive'

  function handleUpdate(values: DriverFormInput) {
    updateMutation.mutate(values, { onSuccess: () => setIsEditOpen(false) })
  }

  function handleConfirm() {
    const mutation = isArchive ? archiveMutation : restoreMutation
    mutation.mutate(undefined, { onSuccess: () => setConfirm(null) })
  }

  if (!isActive && !isArchived) return null

  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="outline">
            <MoreHorizontal />
            Actions
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-44">
          {isActive ? (
            <>
              <DropdownMenuItem onSelect={() => setIsEditOpen(true)}>
                <Pencil />
                Edit details
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                variant="destructive"
                onSelect={() => setConfirm('archive')}
              >
                <Archive />
                Archive driver
              </DropdownMenuItem>
            </>
          ) : (
            <DropdownMenuItem onSelect={() => setConfirm('restore')}>
              <ArchiveRestore />
              Restore driver
            </DropdownMenuItem>
          )}
        </DropdownMenuContent>
      </DropdownMenu>

      <Dialog
        open={isEditOpen}
        onOpenChange={(open) => {
          setIsEditOpen(open)
          if (!open) updateMutation.reset()
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit {driver.name}</DialogTitle>
          </DialogHeader>
          <DriverForm
            defaultValues={{ name: driver.name, contact: driver.contact ?? '' }}
            onSubmit={handleUpdate}
            isSubmitting={updateMutation.isPending}
            error={updateMutation.error}
            submitLabel="Save changes"
          />
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={confirm !== null}
        onOpenChange={(open) => {
          if (!open) setConfirm(null)
        }}
        title={isArchive ? `Archive ${driver.name}?` : `Restore ${driver.name}?`}
        description={
          isArchive
            ? 'Their plates stop being recognised as this driver at the gate. You can restore them later.'
            : 'The driver becomes active again with their existing plates and grants.'
        }
        confirmLabel={isArchive ? 'Archive driver' : 'Restore driver'}
        pendingLabel={isArchive ? 'Archiving…' : 'Restoring…'}
        destructive={isArchive}
        isPending={archiveMutation.isPending || restoreMutation.isPending}
        onConfirm={handleConfirm}
      />
    </>
  )
}
