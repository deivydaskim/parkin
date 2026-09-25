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
import { useLotLayout } from '@/features/lot-view/queries'
import { ReservationDialog } from '@/features/reservations/components/ReservationDialog'
import { toUpdateSpaceInput } from '../api'
import { spaceToFormValues } from '../form-values'
import {
  useDeactivateSpace,
  useReactivateSpace,
  useUpdateSpace,
} from '../queries'
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

function formatPosition(space: Space) {
  if (!space.placement) return 'Unplaced'
  const { x, y, rotationDegrees, level } = space.placement
  return `${x}, ${y} m · ${rotationDegrees}° · L${level}`
}

export function SpaceTable({ lotId, spaces }: Props) {
  const { data: layoutView } = useLotLayout(lotId)
  const assigneeBySpaceId = new Map(
    (layoutView?.spaces ?? []).map((space) => [
      space.id,
      space.reservation?.driverName ?? null,
    ]),
  )

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
          <TableHead>Zone</TableHead>
          <TableHead>Position</TableHead>
          <TableHead>Assignee</TableHead>
          <TableHead />
        </TableRow>
      </TableHeader>
      <TableBody>
        {spaces.map((space) => (
          <SpaceRow
            key={space.id}
            lotId={lotId}
            space={space}
            assignee={assigneeBySpaceId.get(space.id) ?? null}
          />
        ))}
      </TableBody>
    </Table>
  )
}

type SpaceRowProps = {
  lotId: string
  space: Space
  assignee: string | null
}

function SpaceRow({ lotId, space, assignee }: SpaceRowProps) {
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false)
  const updateSpaceMutation = useUpdateSpace(space.id, lotId)
  const deactivateSpaceMutation = useDeactivateSpace(space.id, lotId)
  const reactivateSpaceMutation = useReactivateSpace(space.id, lotId)

  function handleUpdate(values: SpaceFormInput) {
    updateSpaceMutation.mutate(
      toUpdateSpaceInput(values, space.placement !== null),
      {
        onSuccess: () => {
          setIsEditDialogOpen(false)
          updateSpaceMutation.reset()
        },
      },
    )
  }

  return (
    <TableRow>
      <TableCell className="font-medium">{space.label}</TableCell>
      <TableCell>{typeLabel[space.type]}</TableCell>
      <TableCell>{statusLabel[space.status]}</TableCell>
      <TableCell>{space.zone ?? '—'}</TableCell>
      <TableCell className="text-xs tabular-nums">
        {formatPosition(space)}
      </TableCell>
      <TableCell>
        {space.type === 'Reserved' ? (assignee ?? 'Unassigned') : '—'}
      </TableCell>
      <TableCell className="flex justify-end gap-2 text-right">
        {space.type === 'Reserved' && (
          <ReservationDialog
            lotId={lotId}
            spaceId={space.id}
            spaceLabel={space.label}
          />
        )}

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
                defaultValues={spaceToFormValues(space)}
                onSubmit={handleUpdate}
                isSubmitting={updateSpaceMutation.isPending}
                error={updateSpaceMutation.error}
                submitLabel="Save changes"
              />
            </DialogContent>
          </Dialog>
        </RoleGate>

        {space.status === 'Active' ? (
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
        ) : (
          <RoleGate roles={['Operator', 'SystemAdmin']}>
            <Button
              size="sm"
              disabled={reactivateSpaceMutation.isPending}
              onClick={() => reactivateSpaceMutation.mutate()}
            >
              {reactivateSpaceMutation.isPending
                ? 'Reactivating…'
                : 'Reactivate'}
            </Button>
          </RoleGate>
        )}
      </TableCell>
    </TableRow>
  )
}
