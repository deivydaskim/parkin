import { useState } from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
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
import { LotForm } from '@/features/lots/components/LotForm'
import {
  useArchiveLot,
  useLot,
  useRestoreLot,
  useUpdateLot,
} from '@/features/lots/queries'
import { LotStatus, type LotFormInput } from '@/features/lots/schemas'
import { SpaceForm } from '@/features/spaces/components/SpaceForm'
import { SpaceTable } from '@/features/spaces/components/SpaceTable'
import { useCreateSpace, useSpaces } from '@/features/spaces/queries'
import type { SpaceFormInput, SpaceListParams } from '@/features/spaces/schemas'

const spaceStatusFilterOptions: Array<{
  value: NonNullable<SpaceListParams['status']>
  label: string
}> = [
  { value: 'Active', label: 'Active' },
  { value: 'Inactive', label: 'Inactive' },
  { value: 'All', label: 'All' },
]

export const Route = createFileRoute('/_authenticated/lots/$lotId')({
  component: LotDetailPage,
})

function LotDetailPage() {
  const { lotId } = Route.useParams()
  const { data: lot, isLoading } = useLot(lotId)
  const updateLotMutation = useUpdateLot(lotId)
  const archiveLotMutation = useArchiveLot(lotId)
  const restoreLotMutation = useRestoreLot(lotId)

  const [spaceStatus, setSpaceStatus] =
    useState<NonNullable<SpaceListParams['status']>>('Active')
  const { data: spacesData, isLoading: isLoadingSpaces } = useSpaces(lotId, {
    status: spaceStatus,
  })
  const [isSpaceDialogOpen, setIsSpaceDialogOpen] = useState(false)
  const createSpaceMutation = useCreateSpace(lotId)

  function handleUpdate(values: LotFormInput) {
    updateLotMutation.mutate(values)
  }

  function handleCreateSpace(values: SpaceFormInput) {
    createSpaceMutation.mutate(values, {
      onSuccess: () => {
        setIsSpaceDialogOpen(false)
        createSpaceMutation.reset()
      },
    })
  }

  if (isLoading) {
    return <p className="p-6 text-sm text-muted-foreground">Loading…</p>
  }

  if (!lot) {
    return <p className="p-6 text-sm text-muted-foreground">Lot not found.</p>
  }

  return (
    <div className="p-6">
      <div className="mb-4 flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold">{lot.name}</h1>
          <p className="text-sm text-muted-foreground">
            Status: {lot.status === LotStatus.Archived ? 'Archived' : 'Active'}
          </p>
        </div>

        <RoleGate roles={['Operator', 'SystemAdmin']}>
          {lot.status === LotStatus.Archived ? (
            <Button
              disabled={restoreLotMutation.isPending}
              onClick={() => restoreLotMutation.mutate()}
            >
              {restoreLotMutation.isPending ? 'Restoring…' : 'Restore'}
            </Button>
          ) : (
            <Button
              variant="destructive"
              disabled={archiveLotMutation.isPending}
              onClick={() => archiveLotMutation.mutate()}
            >
              {archiveLotMutation.isPending ? 'Archiving…' : 'Archive'}
            </Button>
          )}
        </RoleGate>
      </div>

      <div className="max-w-sm">
        <LotForm
          defaultValues={{
            name: lot.name,
            address: lot.address ?? '',
            timezone: lot.timezone,
            accessMode: lot.accessMode,
            fullBehavior: lot.fullBehavior,
          }}
          onSubmit={handleUpdate}
          isSubmitting={updateLotMutation.isPending}
          error={updateLotMutation.error}
          submitLabel="Save changes"
        />
      </div>

      <div className="mt-8">
        <div className="mb-4 flex items-center justify-between gap-4">
          <h2 className="text-lg font-semibold">Spaces</h2>

          <div className="flex items-center gap-4">
            <Select
              value={spaceStatus}
              onValueChange={(next) =>
                setSpaceStatus(next as NonNullable<SpaceListParams['status']>)
              }
            >
              <SelectTrigger className="w-36">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {spaceStatusFilterOptions.map((option) => (
                  <SelectItem key={option.value} value={option.value}>
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            <RoleGate roles={['Operator', 'SystemAdmin']}>
              <Dialog
                open={isSpaceDialogOpen}
                onOpenChange={setIsSpaceDialogOpen}
              >
                <DialogTrigger asChild>
                  <Button>New space</Button>
                </DialogTrigger>
                <DialogContent>
                  <DialogHeader>
                    <DialogTitle>Create parking space</DialogTitle>
                  </DialogHeader>
                  <SpaceForm
                    onSubmit={handleCreateSpace}
                    isSubmitting={createSpaceMutation.isPending}
                    error={createSpaceMutation.error}
                    submitLabel="Create space"
                  />
                </DialogContent>
              </Dialog>
            </RoleGate>
          </div>
        </div>

        {isLoadingSpaces ? (
          <p className="text-sm text-muted-foreground">Loading…</p>
        ) : (
          <SpaceTable lotId={lotId} spaces={spacesData?.items ?? []} />
        )}
      </div>
    </div>
  )
}
