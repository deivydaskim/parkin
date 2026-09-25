import { Link } from '@tanstack/react-router'
import { ChevronRight, MapPin, SquareParking } from 'lucide-react'
import { Card } from '@/components/ui/card'
import { StatusBadge } from '@/components/StatusBadge'
import { LotStatus, type Lot } from '../schemas'
import { fullBehaviorHints } from '../labels'

type Props = {
  lot: Lot
}

export function LotCard({ lot }: Props) {
  const isArchived = lot.status === LotStatus.Archived

  return (
    <Link
      to="/lots/$lotId"
      params={{ lotId: lot.id }}
      className="group rounded-xl focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
    >
      <Card
        className={
          isArchived
            ? 'h-full gap-4 p-5 opacity-75 transition-shadow group-hover:shadow-md'
            : 'h-full gap-4 p-5 transition-shadow group-hover:shadow-md'
        }
      >
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0 space-y-1">
            <p className="truncate font-semibold">{lot.name}</p>
            <p className="flex items-center gap-1 truncate text-sm text-muted-foreground">
              <MapPin className="size-3.5 shrink-0" aria-hidden />
              <span className="truncate">{lot.address ?? 'No address'}</span>
            </p>
          </div>
          <ChevronRight className="mt-0.5 size-4 shrink-0 text-muted-foreground transition-transform group-hover:translate-x-0.5" />
        </div>

        <div className="flex flex-wrap items-center gap-1.5">
          <StatusBadge status={lot.accessMode} />
          {isArchived ? <StatusBadge status="Archived" /> : null}
        </div>

        <div className="mt-auto flex items-end justify-between gap-3 border-t pt-4">
          <div>
            <p className="flex items-center gap-1.5 text-2xl font-semibold tabular-nums">
              <SquareParking className="size-5 text-primary" aria-hidden />
              {lot.capacity}
            </p>
            <p className="text-xs text-muted-foreground">general spaces</p>
          </div>
          <p className="max-w-[55%] text-right text-xs text-muted-foreground">
            {fullBehaviorHints[lot.fullBehavior]}
          </p>
        </div>
      </Card>
    </Link>
  )
}
