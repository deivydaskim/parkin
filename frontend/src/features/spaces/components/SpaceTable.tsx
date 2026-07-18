import { useState } from 'react'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { RoleGate } from '@/features/auth/components/RoleGate'
import { useDeactivateSpace, useUpdateSpace } from '../queries'
import type { Space, SpaceFormInput } from '../schemas'
import { SpaceForm } from './SpaceForm'

type Props = {
  lotId: string
  spaces: Space[]
}

const typeLabel: Record<Space['type'], string> = {
  General: 'General',
  Reserved: 'Reserved',
}

const statusLabel: Record<Space['status'], string> = {
  Active: 'Active',
  Inactive: 'Inactive',
}

export function SpaceTable({ lotId, spaces }: Props) {
  if (spaces.length === 0) {
    return <p className="text-sm text-muted-foreground">No spaces found.</p>
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Label</TableHead>
          <TableHead>Type</TableHead>
          <TableHead>Status</TableHead>
          <TableHead />
        </TableRow>
      </TableHeader>
      <TableBody>
        {spaces.map((space) => (
          <SpaceRow key={space.id} lotId={lotId} space={space} />
        ))}
      </TableBody>
    </Table>
  )
}

function SpaceRow({ lotId, space }: { lotId: string; space: Space }) {
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false)
  const updateSpaceMutation = useUpdateSpace(space.id, lotId)
  const deactivateSpaceMutation = useDeactivateSpace(space.id, lotId)

  function handleUpdate(values: SpaceFormInput) {
    updateSpaceMutation.mutate(values, {
      onSuccess: () => {
        setIsEditDialogOpen(false)
        updateSpaceMutation.reset()
      },
    })
  }

  return (
    <TableRow>
      <TableCell className="font-medium">{space.label}</TableCell>
      <TableCell>{typeLabel[space.type]}</TableCell>
      <TableCell>{statusLabel[space.status]}</TableCell>
      <TableCell className="flex justify-end gap-2 text-right">
        <RoleGate roles={['Operator', 'SystemAdmin']}>
          <Dialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen}>
            <DialogTrigger asChild>
              <Button variant="outline" size="sm">
                Edit
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Edit parking space</DialogTitle>
              </DialogHeader>
              <SpaceForm
                defaultValues={{ label: space.label, type: space.type }}
                onSubmit={handleUpdate}
                isSubmitting={updateSpaceMutation.isPending}
                error={updateSpaceMutation.error}
                submitLabel="Save changes"
              />
            </DialogContent>
          </Dialog>
        </RoleGate>

        {space.status === 'Active' && (
          <RoleGate roles={['Operator', 'SystemAdmin']}>
            <Button
              variant="destructive"
              size="sm"
              disabled={deactivateSpaceMutation.isPending}
              onClick={() => deactivateSpaceMutation.mutate()}
            >
              {deactivateSpaceMutation.isPending
                ? 'Deactivating…'
                : 'Deactivate'}
            </Button>
          </RoleGate>
        )}
      </TableCell>
    </TableRow>
  )
}
