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
import { useArchiveLot, useRestoreLot, useUpdateLot } from '../queries'
import { LotStatus, type Lot, type LotFormInput } from '../schemas'
import { lotToFormValues } from '../form-values'
import { LotForm } from './LotForm'

type Props = {
  lot: Lot
}

export function LotActions({ lot }: Props) {
  const [isEditOpen, setIsEditOpen] = useState(false)
  const [confirm, setConfirm] = useState<'archive' | 'restore' | null>(null)
  const updateMutation = useUpdateLot(lot.id)
  const archiveMutation = useArchiveLot(lot.id)
  const restoreMutation = useRestoreLot(lot.id)
  const isArchived = lot.status === LotStatus.Archived

  function handleUpdate(values: LotFormInput) {
    updateMutation.mutate(values, {
      onSuccess: () => setIsEditOpen(false),
    })
  }

  function handleConfirm() {
    const mutation = confirm === 'archive' ? archiveMutation : restoreMutation
    mutation.mutate(undefined, { onSuccess: () => setConfirm(null) })
  }

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
          <DropdownMenuItem onSelect={() => setIsEditOpen(true)}>
            <Pencil />
            Edit details
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          {isArchived ? (
            <DropdownMenuItem onSelect={() => setConfirm('restore')}>
              <ArchiveRestore />
              Restore lot
            </DropdownMenuItem>
          ) : (
            <DropdownMenuItem
              variant="destructive"
              onSelect={() => setConfirm('archive')}
            >
              <Archive />
              Archive lot
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
        <DialogContent className="max-h-[90svh] overflow-y-auto sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Edit {lot.name}</DialogTitle>
          </DialogHeader>
          <LotForm
            defaultValues={lotToFormValues(lot)}
            onSubmit={handleUpdate}
            isSubmitting={updateMutation.isPending}
            error={updateMutation.error}
            submitLabel="Save changes"
          />
        </DialogContent>
      </Dialog>

      <LotStatusConfirm
        lot={lot}
        action={confirm}
        onClose={() => setConfirm(null)}
        onConfirm={handleConfirm}
        isPending={archiveMutation.isPending || restoreMutation.isPending}
      />
    </>
  )
}

type LotStatusConfirmProps = {
  lot: Lot
  action: 'archive' | 'restore' | null
  onClose: () => void
  onConfirm: () => void
  isPending: boolean
}

export function LotStatusConfirm({
  lot,
  action,
  onClose,
  onConfirm,
  isPending,
}: LotStatusConfirmProps) {
  const isArchive = action === 'archive'

  return (
    <ConfirmDialog
      open={action !== null}
      onOpenChange={(open) => {
        if (!open) onClose()
      }}
      title={isArchive ? `Archive ${lot.name}?` : `Restore ${lot.name}?`}
      description={
        isArchive
          ? 'The gate will deny every new entry at this lot. Cars already inside can still exit, and you can restore it later.'
          : 'The lot starts accepting entries again with its current spaces and rules.'
      }
      confirmLabel={isArchive ? 'Archive lot' : 'Restore lot'}
      pendingLabel={isArchive ? 'Archiving…' : 'Restoring…'}
      destructive={isArchive}
      isPending={isPending}
      onConfirm={onConfirm}
    />
  )
}
