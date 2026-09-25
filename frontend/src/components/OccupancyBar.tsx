import { Progress } from '@/components/ui/progress'
import { cn } from '@/lib/utils'

type Props = {
  used: number
  capacity: number
  className?: string
  showLabel?: boolean
}

function indicatorClassFor(ratio: number, isOver: boolean) {
  if (isOver) return 'bg-stripes-destructive'
  if (ratio < 0.7) return 'bg-success'
  if (ratio < 0.9) return 'bg-warning'
  return 'bg-destructive'
}

export function OccupancyBar({
  used,
  capacity,
  className,
  showLabel = true,
}: Props) {
  const isOver = used > capacity
  const ratio = capacity > 0 ? used / capacity : used > 0 ? 1 : 0
  const percent = Math.min(100, Math.round(ratio * 100))

  return (
    <div className={cn('space-y-1.5', className)}>
      {showLabel ? (
        <div className="flex items-baseline justify-between text-xs text-muted-foreground">
          <span className="tabular-nums">
            <span className="font-medium text-foreground">{used}</span> /{' '}
            {capacity} general used
          </span>
          <span
            className={cn(
              'font-medium tabular-nums',
              isOver && 'text-destructive',
            )}
          >
            {capacity > 0 ? `${Math.round(ratio * 100)}%` : '—'}
          </span>
        </div>
      ) : null}
      <Progress
        value={percent}
        aria-label={`${used} of ${capacity} general spaces used`}
        className="h-2.5 bg-muted"
        indicatorClassName={indicatorClassFor(ratio, isOver)}
      />
    </div>
  )
}
