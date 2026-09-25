import { Link } from '@tanstack/react-router'
import { Activity, LogIn, LogOut } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { PlateChip } from '@/components/PlateChip'
import { StatusBadge } from '@/components/StatusBadge'
import { formatDateTime, formatRelative } from '@/lib/format'
import { cn } from '@/lib/utils'
import { describeOutcome } from '../decision'
import { useAccessEvents } from '../queries'
import { Decision, Direction, type AccessEvent } from '../schemas'

type Props = {
  lotId: string
  limit?: number
  className?: string
}

export function RecentActivity({ lotId, limit = 10, className }: Props) {
  const { data, isLoading, isError } = useAccessEvents(lotId, {
    perPage: limit,
  })
  const events = data?.items ?? []

  return (
    <Card className={cn('gap-0 py-0', className)}>
      <CardHeader className="flex flex-row items-center justify-between border-b py-4 [.border-b]:pb-4">
        <CardTitle className="flex items-center gap-2 text-base">
          <Activity className="size-4 text-primary" aria-hidden />
          Recent activity
        </CardTitle>
        {data ? (
          <span className="text-xs text-muted-foreground tabular-nums">
            {data.totalCount} total
          </span>
        ) : null}
      </CardHeader>
      <CardContent className="px-0">
        {isLoading ? (
          <ul className="divide-y">
            {Array.from({ length: 4 }, (_, index) => (
              <li key={index} className="flex items-center gap-3 px-4 py-3">
                <Skeleton className="size-8 rounded-full" />
                <div className="flex-1 space-y-1.5">
                  <Skeleton className="h-3.5 w-24" />
                  <Skeleton className="h-3 w-40" />
                </div>
              </li>
            ))}
          </ul>
        ) : isError ? (
          <p className="px-4 py-6 text-sm text-destructive">
            Could not load recent activity.
          </p>
        ) : events.length === 0 ? (
          <p className="px-4 py-8 text-center text-sm text-muted-foreground">
            No entries or exits recorded yet.
          </p>
        ) : (
          <ul className="divide-y">
            {events.map((event) => (
              <ActivityRow key={event.id} event={event} />
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  )
}

function ActivityRow({ event }: { event: AccessEvent }) {
  const isEntry = event.direction === Direction.Enter
  const DirectionIcon = isEntry ? LogIn : LogOut
  const isAllowed = event.decision === Decision.Allow

  return (
    <li className="flex items-center gap-3 px-4 py-3">
      <span
        className={cn(
          'flex size-8 shrink-0 items-center justify-center rounded-full',
          isAllowed
            ? 'bg-success/12 text-success'
            : 'bg-destructive/10 text-destructive',
        )}
        title={isEntry ? 'Entry' : 'Exit'}
      >
        <DirectionIcon className="size-4" aria-hidden />
        <span className="sr-only">{isEntry ? 'Entry' : 'Exit'}</span>
      </span>
      <div className="min-w-0 flex-1 space-y-1">
        <div className="flex flex-wrap items-center gap-2">
          <PlateChip plate={event.plate} className="text-xs" />
          {event.driverId && event.driverName ? (
            <Link
              to="/drivers/$driverId"
              params={{ driverId: event.driverId }}
              className="truncate text-sm font-medium hover:underline"
            >
              {event.driverName}
            </Link>
          ) : (
            <span className="text-sm text-muted-foreground">Unknown plate</span>
          )}
        </div>
        <p className="truncate text-xs text-muted-foreground">
          {describeOutcome(event)}
          {event.source === 'Manual' ? ' · Manual' : ' · Gate'}
        </p>
      </div>
      <div className="flex shrink-0 flex-col items-end gap-1">
        <StatusBadge status={event.decision} hideIcon />
        <time
          dateTime={event.occurredAt}
          title={formatDateTime(event.occurredAt)}
          className="text-xs text-muted-foreground"
        >
          {formatRelative(event.occurredAt)}
        </time>
      </div>
    </li>
  )
}
