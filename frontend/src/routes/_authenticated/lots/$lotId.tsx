import { createFileRoute, Link, useNavigate } from '@tanstack/react-router'
import { Box, DoorOpen, MapPin, ParkingSquare } from 'lucide-react'
import { z } from 'zod'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { EmptyState } from '@/components/EmptyState'
import { PageHeader } from '@/components/PageHeader'
import { StatusBadge } from '@/components/StatusBadge'
import { RoleGate } from '@/features/auth/components/RoleGate'
import { useHasRole } from '@/features/auth/permissions'
import { ManualEntryForm } from '@/features/gate/components/ManualEntryForm'
import { RecentActivity } from '@/features/gate/components/RecentActivity'
import { LotActions } from '@/features/lots/components/LotActions'
import { LotCrumb } from '@/features/lots/components/LotCrumb'
import { LotSettingsPanel } from '@/features/lots/components/LotSettingsPanel'
import { fullBehaviorLabels } from '@/features/lots/labels'
import { useLot } from '@/features/lots/queries'
import { LotStatus } from '@/features/lots/schemas'
import { OccupancyStats } from '@/features/occupancy/components/OccupancyStats'
import {
  SpacesPanel,
  type SpaceFilters,
} from '@/features/spaces/components/SpacesPanel'

const tabs = ['overview', 'spaces', 'settings'] as const

const lotSearchSchema = z.object({
  tab: z.enum(tabs).optional().catch(undefined),
  status: z.enum(['Active', 'Inactive', 'All']).optional().catch(undefined),
  type: z.enum(['All', 'General', 'Reserved']).optional().catch(undefined),
  q: z.string().optional().catch(undefined),
  view: z.enum(['grid', 'table']).optional().catch(undefined),
  page: z.number().int().min(1).optional().catch(undefined),
})

export const Route = createFileRoute('/_authenticated/lots/$lotId')({
  validateSearch: lotSearchSchema,
  staticData: {
    crumb: LotCrumb,
    parentCrumbs: [{ label: 'Parking lots', to: '/lots' }],
  },
  component: LotDetailPage,
})

function LotDetailPage() {
  const { lotId } = Route.useParams()
  const search = Route.useSearch()
  const navigate = useNavigate({ from: Route.fullPath })
  const { data: lot, isLoading } = useLot(lotId)
  const canOperate = useHasRole('Operator', 'SystemAdmin')

  const tab = search.tab ?? 'overview'
  const spaceFilters: SpaceFilters = {
    status: search.status ?? 'Active',
    type: search.type ?? 'All',
    q: search.q ?? '',
    view: search.view ?? 'grid',
    page: search.page ?? 1,
  }

  function setSpaceFilters(next: Partial<SpaceFilters>) {
    const merged = { ...spaceFilters, ...next }
    navigate({
      search: (prev) => ({
        ...prev,
        status: merged.status === 'Active' ? undefined : merged.status,
        type: merged.type === 'All' ? undefined : merged.type,
        q: merged.q || undefined,
        view: merged.view === 'grid' ? undefined : merged.view,
        page: merged.page === 1 ? undefined : merged.page,
      }),
      replace: true,
    })
  }

  if (isLoading) {
    return (
      <div className="space-y-6" aria-busy>
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-4 w-80" />
        <Skeleton className="h-9 w-72" />
        <Skeleton className="h-48 w-full rounded-xl" />
      </div>
    )
  }

  if (!lot) {
    return (
      <EmptyState
        icon={ParkingSquare}
        title="Lot not found"
        hint="It may have been removed, or the link is wrong."
        action={
          <Button variant="outline" asChild>
            <Link to="/lots">Back to lots</Link>
          </Button>
        }
      />
    )
  }

  const isActive = lot.status === LotStatus.Active

  return (
    <div className="space-y-6">
      <PageHeader
        title={lot.name}
        description={
          <span className="flex items-center gap-1.5">
            <MapPin className="size-3.5" aria-hidden />
            {lot.address ?? 'No address'} · {lot.timezone}
          </span>
        }
        badges={
          <>
            <StatusBadge status={lot.status} />
            <StatusBadge status={lot.accessMode} />
            <Badge variant="outline">{fullBehaviorLabels[lot.fullBehavior]}</Badge>
            <Badge variant="outline" className="tabular-nums">
              {lot.capacity} general spaces
            </Badge>
          </>
        }
        actions={
          <>
            <Button variant="outline" asChild>
              <Link to="/lots/$lotId/map" params={{ lotId }}>
                <Box />
                3D layout
              </Link>
            </Button>
            <RoleGate roles={['Operator', 'SystemAdmin']}>
              <LotActions lot={lot} />
            </RoleGate>
          </>
        }
      />

      <Tabs
        value={tab}
        onValueChange={(next) =>
          navigate({
            search: (prev) => ({
              ...prev,
              tab: next === 'overview' ? undefined : (next as (typeof tabs)[number]),
            }),
            replace: true,
          })
        }
      >
        <TabsList>
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="spaces">Spaces</TabsTrigger>
          {canOperate ? <TabsTrigger value="settings">Settings</TabsTrigger> : null}
        </TabsList>

        <TabsContent value="overview" className="mt-4 space-y-6">
          <OccupancyStats lotId={lotId} />
          <div className="grid gap-6 lg:grid-cols-2">
            <RoleGate roles={['Operator', 'SystemAdmin']}>
              <Card className="h-fit">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2 text-base">
                    <DoorOpen className="size-4 text-primary" aria-hidden />
                    Manual entry / exit
                  </CardTitle>
                  <CardDescription>
                    For plate-reader failures and walk-ups.{' '}
                    <Link
                      to="/gate"
                      search={{ lot: lotId }}
                      className="font-medium text-primary hover:underline"
                    >
                      Open gate console
                    </Link>
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  {isActive ? (
                    <ManualEntryForm lotId={lotId} />
                  ) : (
                    <p className="text-sm text-muted-foreground">
                      Archived lots deny all entries. Restore the lot to record
                      events.
                    </p>
                  )}
                </CardContent>
              </Card>
            </RoleGate>
            <RecentActivity lotId={lotId} limit={6} />
          </div>
        </TabsContent>

        <TabsContent value="spaces" className="mt-4">
          <SpacesPanel
            lotId={lotId}
            filters={spaceFilters}
            onFiltersChange={setSpaceFilters}
            canCreate={isActive}
          />
        </TabsContent>

        {canOperate ? (
          <TabsContent value="settings" className="mt-4">
            <LotSettingsPanel lot={lot} />
          </TabsContent>
        ) : null}
      </Tabs>
    </div>
  )
}
