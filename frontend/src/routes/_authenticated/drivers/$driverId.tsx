import { createFileRoute, Link, useNavigate } from '@tanstack/react-router'
import { Car } from 'lucide-react'
import { z } from 'zod'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { EmptyState } from '@/components/EmptyState'
import { PageHeader } from '@/components/PageHeader'
import { StatusBadge } from '@/components/StatusBadge'
import { RoleGate } from '@/features/auth/components/RoleGate'
import { DriverActions } from '@/features/drivers/components/DriverActions'
import { DriverCrumb } from '@/features/drivers/components/DriverCrumb'
import { DriverGrantsPanel } from '@/features/drivers/components/DriverGrantsPanel'
import { PlateManager } from '@/features/drivers/components/PlateManager'
import { useDriver } from '@/features/drivers/queries'
import { DriverStatus } from '@/features/drivers/schemas'
import { initialsOf } from '@/lib/format'

const tabs = ['plates', 'access'] as const

const driverSearchSchema = z.object({
  tab: z.enum(tabs).optional().catch(undefined),
})

export const Route = createFileRoute('/_authenticated/drivers/$driverId')({
  validateSearch: driverSearchSchema,
  staticData: {
    crumb: DriverCrumb,
    parentCrumbs: [{ label: 'Drivers', to: '/drivers' }],
  },
  component: DriverDetailPage,
})

function DriverDetailPage() {
  const { driverId } = Route.useParams()
  const { tab = 'plates' } = Route.useSearch()
  const navigate = useNavigate({ from: Route.fullPath })
  const { data: driver, isLoading } = useDriver(driverId)

  if (isLoading) {
    return (
      <div className="space-y-6" aria-busy>
        <div className="flex items-center gap-4">
          <Skeleton className="size-14 rounded-full" />
          <div className="space-y-2">
            <Skeleton className="h-7 w-48" />
            <Skeleton className="h-4 w-32" />
          </div>
        </div>
        <Skeleton className="h-40 w-full rounded-xl" />
      </div>
    )
  }

  if (!driver) {
    return (
      <EmptyState
        icon={Car}
        title="Driver not found"
        hint="They may have been removed, or the link is wrong."
        action={
          <Button variant="outline" asChild>
            <Link to="/drivers">Back to drivers</Link>
          </Button>
        }
      />
    )
  }

  const canEdit = driver.status === DriverStatus.Active

  return (
    <div className="space-y-6">
      <PageHeader
        leading={
          <Avatar className="size-14">
            <AvatarFallback className="bg-primary text-lg font-semibold text-primary-foreground">
              {initialsOf(driver.name)}
            </AvatarFallback>
          </Avatar>
        }
        title={driver.name}
        description={driver.contact ?? 'No contact details'}
        badges={
          <>
            <StatusBadge status={driver.status} />
            <span className="text-xs text-muted-foreground tabular-nums">
              {driver.plateCount} plate{driver.plateCount === 1 ? '' : 's'}
            </span>
          </>
        }
        actions={
          <RoleGate roles={['Operator', 'SystemAdmin']}>
            <DriverActions driver={driver} />
          </RoleGate>
        }
      />

      {!canEdit ? (
        <p className="rounded-lg border bg-muted/50 p-3 text-sm text-muted-foreground">
          This driver is {driver.status.toLowerCase()}. Restore them to change
          plates or lot access.
        </p>
      ) : null}

      <Tabs
        value={tab}
        onValueChange={(next) =>
          navigate({
            search: { tab: next === 'plates' ? undefined : (next as (typeof tabs)[number]) },
            replace: true,
          })
        }
      >
        <TabsList>
          <TabsTrigger value="plates">Plates</TabsTrigger>
          <TabsTrigger value="access">Lot access</TabsTrigger>
        </TabsList>
        <TabsContent value="plates" className="mt-4">
          <PlateManager driverId={driverId} canEdit={canEdit} />
        </TabsContent>
        <TabsContent value="access" className="mt-4">
          <DriverGrantsPanel driverId={driverId} canEdit={canEdit} />
        </TabsContent>
      </Tabs>
    </div>
  )
}
