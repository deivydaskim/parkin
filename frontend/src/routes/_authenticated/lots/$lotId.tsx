import { createFileRoute } from '@tanstack/react-router'
import { Button } from '@/components/ui/button'
import { RoleGate } from '@/features/auth/components/RoleGate'
import { LotForm } from '@/features/lots/components/LotForm'
import { useArchiveLot, useLot, useUpdateLot } from '@/features/lots/queries'
import type { LotFormInput } from '@/features/lots/schemas'

export const Route = createFileRoute('/_authenticated/lots/$lotId')({
  component: LotDetailPage,
})

function LotDetailPage() {
  const { lotId } = Route.useParams()
  const { data: lot, isLoading } = useLot(lotId)
  const updateLotMutation = useUpdateLot(lotId)
  const archiveLotMutation = useArchiveLot(lotId)

  function handleUpdate(values: LotFormInput) {
    updateLotMutation.mutate(values)
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
            Status: {lot.status === 'ARCHIVED' ? 'Archived' : 'Active'}
          </p>
        </div>

        <RoleGate roles={['Operator', 'SystemAdmin']}>
          <Button
            variant="destructive"
            disabled={lot.status === 'ARCHIVED' || archiveLotMutation.isPending}
            onClick={() => archiveLotMutation.mutate()}
          >
            {archiveLotMutation.isPending ? 'Archiving…' : 'Archive'}
          </Button>
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
    </div>
  )
}
