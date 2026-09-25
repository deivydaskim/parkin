import { Star, UserRound } from 'lucide-react'
import { cn } from '@/lib/utils'
import { SpaceStatus, SpaceType, type Space } from '../schemas'

type Props = {
  space: Space
  onSelect: (space: Space) => void
}

export function SpaceTile({ space, onSelect }: Props) {
  const isInactive = space.status === SpaceStatus.Inactive
  const isReserved = space.type === SpaceType.Reserved

  return (
    <button
      type="button"
      onClick={() => onSelect(space)}
      className={cn(
        'group relative flex aspect-[4/5] min-w-0 flex-col justify-between rounded-lg border-2 p-2.5 text-left transition-all hover:-translate-y-0.5 hover:shadow-md focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background focus-visible:outline-none',
        isInactive
          ? 'border-dashed border-muted-foreground/40 bg-muted/50 text-muted-foreground'
          : isReserved
            ? 'border-warning/60 bg-warning/15'
            : 'border-primary/40 bg-primary/8',
      )}
      aria-label={`Space ${space.label}, ${isInactive ? 'inactive' : space.type.toLowerCase()}${space.reservedDriverName ? `, reserved for ${space.reservedDriverName}` : ''}`}
    >
      <div className="flex items-start justify-between gap-1">
        <span
          className={cn(
            'truncate font-mono text-sm font-bold',
            !isInactive && (isReserved ? 'text-warning-foreground dark:text-warning' : 'text-primary'),
          )}
        >
          {space.label}
        </span>
        {isReserved && !isInactive ? (
          <Star className="size-3.5 shrink-0 fill-warning text-warning" aria-hidden />
        ) : null}
      </div>

      <div className="min-w-0 space-y-0.5">
        {isInactive ? (
          <span className="text-[0.7rem] font-medium uppercase tracking-wide">
            Inactive
          </span>
        ) : isReserved ? (
          space.reservedDriverName ? (
            <span className="flex items-center gap-1 text-xs font-medium">
              <UserRound className="size-3 shrink-0" aria-hidden />
              <span className="truncate">{space.reservedDriverName}</span>
            </span>
          ) : (
            <span className="text-xs text-muted-foreground">Unassigned</span>
          )
        ) : (
          <span className="text-xs text-muted-foreground">General</span>
        )}
        {space.zone ? (
          <span className="block truncate text-[0.7rem] text-muted-foreground">
            {space.zone}
          </span>
        ) : null}
      </div>
    </button>
  )
}
