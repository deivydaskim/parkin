import { createFileRoute } from '@tanstack/react-router'
import { Button } from '@/components/ui/button'
import { RoleGate } from '@/features/auth/components/RoleGate'
import { DriverForm } from '@/features/drivers/components/DriverForm'
import { PlateManager } from '@/features/drivers/components/PlateManager'
import {
  useArchiveDriver,
  useDriver,
  useUpdateDriver,
} from '@/features/drivers/queries'
import { DriverStatus, type DriverFormInput } from '@/features/drivers/schemas'

export const Route = createFileRoute('/_authenticated/drivers/$driverId')({
  component: DriverDetailPage,
})

function DriverDetailPage() {
  const { driverId } = Route.useParams()
  const { data: driver, isLoading } = useDriver(driverId)
  const updateDriverMutation = useUpdateDriver(driverId)
  const archiveDriverMutation = useArchiveDriver(driverId)

  function handleUpdate(values: DriverFormInput) {
    updateDriverMutation.mutate(values)
  }

  if (isLoading) {
    return <p className="p-6 text-sm text-muted-foreground">Loading…</p>
  }

  if (!driver) {
    return (
      <p className="p-6 text-sm text-muted-foreground">Driver not found.</p>
    )
  }

  return (
    <div className="p-6">
      <div className="mb-4 flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold">{driver.name}</h1>
          <p className="text-sm text-muted-foreground">
            Status: {driver.status}
          </p>
        </div>

        <RoleGate roles={['Operator', 'SystemAdmin']}>
          <Button
            variant="destructive"
            disabled={
              driver.status !== DriverStatus.Active ||
              archiveDriverMutation.isPending
            }
            onClick={() => archiveDriverMutation.mutate()}
          >
            {archiveDriverMutation.isPending ? 'Archiving…' : 'Archive'}
          </Button>
        </RoleGate>
      </div>

      <div className="max-w-sm">
        <DriverForm
          defaultValues={{
            name: driver.name,
            contact: driver.contact ?? '',
          }}
          onSubmit={handleUpdate}
          isSubmitting={updateDriverMutation.isPending}
          error={updateDriverMutation.error}
          submitLabel="Save changes"
        />
      </div>

      <div className="mt-8">
        <h2 className="mb-4 text-lg font-semibold">Plates</h2>
        <PlateManager driverId={driverId} />
      </div>
    </div>
  )
}
