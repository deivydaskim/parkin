import { createFileRoute, Link } from '@tanstack/react-router'
import {
  ArrowRight,
  Car,
  CircleParking,
  DoorOpen,
  ParkingSquare,
  Plus,
  SquareParking,
  UserPlus,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { EmptyState } from '@/components/EmptyState'
import { PageHeader } from '@/components/PageHeader'
import { CardSkeleton } from '@/components/Skeletons'
import { StatCard } from '@/components/StatCard'
import { RoleGate } from '@/features/auth/components/RoleGate'
import { useAuthStore } from '@/features/auth/store'
import { LotOccupancyCard } from '@/features/occupancy/components/LotOccupancyCard'
import { useAllLotOccupancy } from '@/features/occupancy/queries'
import { greetingFor } from '@/lib/format'

export const Route = createFileRoute('/_authenticated/')({
  staticData: { crumb: 'Dashboard' },
  component: DashboardPage,
})

function DashboardPage() {
  const user = useAuthStore((state) => state.user)
  const { data: occupancies, isLoading, isError } = useAllLotOccupancy()
  const lots = occupancies ?? []

  const totals = lots.reduce(
    (sum, lot) => ({
      spaces: sum.spaces + lot.generalCapacity + lot.reservedSpaceCount,
      parked: sum.parked + lot.generalUsed + lot.reservedOccupied,
      free: sum.free + lot.generalFree,
      general: sum.general + lot.generalCapacity,
    }),
    { spaces: 0, parked: 0, free: 0, general: 0 },
  )
  const fullLots = lots.filter((lot) => lot.isGeneralPoolFull).length
  const firstName = user?.displayName.split(' ')[0]

  return (
    <div className="space-y-8">
      <PageHeader
        title={`${greetingFor(new Date())}${firstName ? `, ${firstName}` : ''}`}
        description="Here's how your parking lots look right now."
        actions={
          <RoleGate roles={['Operator', 'SystemAdmin']}>
            <Button asChild>
              <Link to="/gate">
                <DoorOpen />
                Record entry / exit
              </Link>
            </Button>
          </RoleGate>
        }
      />

      <section
        aria-label="Key figures"
        className="grid grid-cols-2 gap-3 sm:gap-4 lg:grid-cols-4"
      >
        <StatCard
          label="Active lots"
          value={isLoading ? '—' : lots.length}
          icon={ParkingSquare}
          tone="primary"
          hint={fullLots > 0 ? `${fullLots} full right now` : 'All accepting cars'}
        />
        <StatCard
          label="Total spaces"
          value={isLoading ? '—' : totals.spaces}
          icon={SquareParking}
          hint={`${totals.general} general · ${totals.spaces - totals.general} reserved`}
        />
        <StatCard
          label="Parked now"
          value={isLoading ? '—' : totals.parked}
          icon={Car}
          tone="warning"
          hint="Open sessions across all lots"
        />
        <StatCard
          label="General free"
          value={isLoading ? '—' : totals.free}
          icon={CircleParking}
          tone="success"
          hint="Spaces open to any permitted driver"
        />
      </section>

      <div className="grid gap-8 xl:grid-cols-[minmax(0,1fr)_18rem]">
        <section aria-labelledby="lots-at-a-glance" className="space-y-4">
          <div className="flex items-center justify-between gap-2">
            <h2 id="lots-at-a-glance" className="text-lg font-semibold">
              Lots at a glance
            </h2>
            <Button variant="ghost" size="sm" asChild>
              <Link to="/lots">
                All lots
                <ArrowRight />
              </Link>
            </Button>
          </div>

          {isLoading ? (
            <CardSkeleton count={4} className="xl:grid-cols-2" />
          ) : isError ? (
            <p className="text-sm text-destructive">
              Could not load live occupancy.
            </p>
          ) : lots.length === 0 ? (
            <EmptyState
              icon={ParkingSquare}
              title="No active lots yet"
              hint="Create a parking lot and add spaces to start tracking occupancy."
              action={
                <Button asChild>
                  <Link to="/lots" search={{ create: true }}>
                    <Plus />
                    Create your first lot
                  </Link>
                </Button>
              }
            />
          ) : (
            <div className="grid gap-4 sm:grid-cols-2">
              {lots.map((lot) => (
                <LotOccupancyCard key={lot.lotId} occupancy={lot} />
              ))}
            </div>
          )}
        </section>

        <aside aria-labelledby="quick-actions" className="space-y-4">
          <h2 id="quick-actions" className="text-lg font-semibold">
            Quick actions
          </h2>
          <Card className="gap-1 p-2">
            <QuickAction
              to="/gate"
              icon={DoorOpen}
              title="Record entry or exit"
              hint="Manual gate for walk-ups and reader failures"
            />
            <QuickAction
              to="/drivers"
              create
              icon={UserPlus}
              title="New driver"
              hint="Register a driver and their plates"
            />
            <QuickAction
              to="/lots"
              create
              icon={Plus}
              title="New lot"
              hint="Set up a lot, its spaces and access rules"
            />
          </Card>
        </aside>
      </div>
    </div>
  )
}

type QuickActionProps = {
  to: '/gate' | '/drivers' | '/lots'
  create?: boolean
  icon: typeof DoorOpen
  title: string
  hint: string
}

function QuickAction({ to, create, icon: Icon, title, hint }: QuickActionProps) {
  const content = (
    <>
      <span className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary">
        <Icon className="size-4.5" />
      </span>
      <span className="min-w-0 flex-1">
        <span className="block text-sm font-medium">{title}</span>
        <span className="block truncate text-xs text-muted-foreground">
          {hint}
        </span>
      </span>
      <ArrowRight className="size-4 text-muted-foreground transition-transform group-hover:translate-x-0.5" />
    </>
  )
  const className =
    'group flex items-center gap-3 rounded-lg p-3 transition-colors hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none'

  if (to === '/gate') {
    return (
      <Link to="/gate" className={className}>
        {content}
      </Link>
    )
  }

  return (
    <Link to={to} search={create ? { create: true } : {}} className={className}>
      {content}
    </Link>
  )
}
