import { Car, CircleParking, SquareParking, Star } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Card } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { OccupancyBar } from '@/components/OccupancyBar'
import { StatCard } from '@/components/StatCard'
import { cn } from '@/lib/utils'
import { useLotOccupancy } from '../queries'
import type { LotOccupancy } from '../schemas'

type Props = {
  lotId: string
  compact?: boolean
}

export function OccupancyStats({ lotId, compact = false }: Props) {
  const { data, isLoading, isError } = useLotOccupancy(lotId)

  if (isLoading) {
    return (
      <div className="space-y-3" aria-busy aria-label="Loading occupancy">
        <Skeleton className="h-5 w-40" />
        <Skeleton className="h-2.5 w-full rounded-full" />
        <div className={cn('grid gap-3', compact ? 'grid-cols-2' : 'sm:grid-cols-4')}>
          {Array.from({ length: 4 }, (_, index) => (
            <Skeleton key={index} className="h-20 rounded-xl" />
          ))}
        </div>
      </div>
    )
  }

  if (isError || !data) {
    return (
      <p className="text-sm text-muted-foreground">
        Could not load occupancy for this lot.
      </p>
    )
  }

  return compact ? <CompactStats data={data} /> : <FullStats data={data} />
}

function StateBadge({ data }: { data: LotOccupancy }) {
  const overBy = data.generalUsed - data.generalCapacity
  if (data.isOverCapacity) {
    return <Badge variant="destructive">Over capacity by {overBy}</Badge>
  }
  if (data.isGeneralPoolFull) {
    return (
      <Badge className="border-destructive/30 bg-destructive/10 text-destructive">
        Full
      </Badge>
    )
  }
  return (
    <Badge className="border-success/30 bg-success/12 text-success">
      Accepting cars
    </Badge>
  )
}

function UpdatedAt({ asOf }: { asOf: string }) {
  return (
    <span className="text-xs text-muted-foreground">
      Updated {new Date(asOf).toLocaleTimeString()}
    </span>
  )
}

function FullStats({ data }: { data: LotOccupancy }) {
  return (
    <div className="space-y-4">
      <Card className="gap-4 p-5">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div className="flex items-center gap-2">
            <h2 className="font-semibold">Live occupancy</h2>
            <StateBadge data={data} />
          </div>
          <UpdatedAt asOf={data.asOf} />
        </div>
        <OccupancyBar used={data.generalUsed} capacity={data.generalCapacity} />
        <p className="text-xs text-muted-foreground">
          Occupancy is lot-level and counted at the gate — not per-space sensed.
        </p>
      </Card>
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <StatCard
          label="General capacity"
          value={data.generalCapacity}
          icon={SquareParking}
          tone="primary"
          hint="Active general spaces"
        />
        <StatCard
          label="General used"
          value={data.generalUsed}
          icon={Car}
          tone={data.isOverCapacity ? 'destructive' : 'default'}
          valueClassName={data.isOverCapacity ? 'text-destructive' : undefined}
          hint="Open general sessions"
        />
        <StatCard
          label="General free"
          value={data.generalFree}
          icon={CircleParking}
          tone={data.generalFree > 0 ? 'success' : 'destructive'}
          hint={data.isGeneralPoolFull ? 'No room in the general pool' : 'Ready for arrivals'}
        />
        <StatCard
          label="Reserved in use"
          value={`${data.reservedOccupied} / ${data.reservedSpaceCount}`}
          icon={Star}
          tone="warning"
          hint="Occupied / reserved spaces"
        />
      </div>
    </div>
  )
}

function CompactStats({ data }: { data: LotOccupancy }) {
  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <StateBadge data={data} />
        <UpdatedAt asOf={data.asOf} />
      </div>
      <OccupancyBar used={data.generalUsed} capacity={data.generalCapacity} />
      <dl className="grid grid-cols-2 gap-3 text-sm">
        <CompactStat label="General free" value={data.generalFree} />
        <CompactStat label="General used" value={data.generalUsed} />
        <CompactStat label="Capacity" value={data.generalCapacity} />
        <CompactStat
          label="Reserved in use"
          value={`${data.reservedOccupied} / ${data.reservedSpaceCount}`}
        />
      </dl>
    </div>
  )
}

function CompactStat({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="rounded-lg bg-muted/60 px-3 py-2">
      <dt className="text-xs text-muted-foreground">{label}</dt>
      <dd className="text-lg font-semibold tabular-nums">{value}</dd>
    </div>
  )
}
