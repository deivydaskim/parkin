import { Link } from '@tanstack/react-router'
import { ChevronRight, Star } from 'lucide-react'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { OccupancyBar } from '@/components/OccupancyBar'
import type { LotOccupancy } from '../schemas'

type Props = {
  occupancy: LotOccupancy
}

export function LotOccupancyCard({ occupancy }: Props) {
  const overBy = occupancy.generalUsed - occupancy.generalCapacity

  return (
    <Link
      to="/lots/$lotId"
      params={{ lotId: occupancy.lotId }}
      className="group rounded-xl focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
    >
      <Card className="h-full gap-4 p-5 transition-shadow group-hover:shadow-md">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <p className="truncate font-semibold">
              {occupancy.lotName ?? 'Unnamed lot'}
            </p>
            <p className="text-sm text-muted-foreground tabular-nums">
              {occupancy.generalFree} general free
            </p>
          </div>
          <div className="flex items-center gap-1.5">
            {occupancy.isOverCapacity ? (
              <Badge variant="destructive">Over by {overBy}</Badge>
            ) : occupancy.isGeneralPoolFull ? (
              <Badge className="border-destructive/30 bg-destructive/10 text-destructive">
                Full
              </Badge>
            ) : null}
            <ChevronRight className="size-4 text-muted-foreground transition-transform group-hover:translate-x-0.5" />
          </div>
        </div>
        <OccupancyBar
          used={occupancy.generalUsed}
          capacity={occupancy.generalCapacity}
        />
        {occupancy.reservedSpaceCount > 0 ? (
          <p className="flex items-center gap-1.5 text-xs text-muted-foreground tabular-nums">
            <Star className="size-3.5 text-warning" aria-hidden />
            {occupancy.reservedOccupied} / {occupancy.reservedSpaceCount}{' '}
            reserved in use
          </p>
        ) : null}
      </Card>
    </Link>
  )
}
