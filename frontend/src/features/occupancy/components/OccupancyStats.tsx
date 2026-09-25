import { useLotOccupancy } from '../queries'

type Props = {
  lotId: string
  compact?: boolean
}

type StatProps = {
  label: string
  value: string
  hint?: string
  emphasis?: 'default' | 'warning'
}

function Stat({ label, value, hint, emphasis = 'default' }: StatProps) {
  return (
    <div className="rounded-lg border p-4">
      <p className="text-xs font-medium tracking-wide text-muted-foreground uppercase">
        {label}
      </p>
      <p
        className={
          emphasis === 'warning'
            ? 'mt-1 text-2xl font-semibold tabular-nums text-destructive'
            : 'mt-1 text-2xl font-semibold tabular-nums'
        }
      >
        {value}
      </p>
      {hint ? (
        <p className="mt-1 text-xs text-muted-foreground">{hint}</p>
      ) : null}
    </div>
  )
}

export function OccupancyStats({ lotId, compact = false }: Props) {
  const { data, isLoading, isError } = useLotOccupancy(lotId)

  if (isLoading) {
    return <p className="text-sm text-muted-foreground">Loading occupancy…</p>
  }

  if (isError || !data) {
    return (
      <p className="text-sm text-muted-foreground">
        Could not load occupancy for this lot.
      </p>
    )
  }

  const overBy = data.generalUsed - data.generalCapacity

  return (
    <div>
      <div className="mb-3 flex flex-wrap items-center gap-3">
        <h2 className="text-lg font-semibold">Live occupancy</h2>

        {data.isOverCapacity ? (
          <span className="rounded-full bg-destructive/10 px-2.5 py-0.5 text-xs font-medium text-destructive">
            Over capacity by {overBy}
          </span>
        ) : data.isGeneralPoolFull ? (
          <span className="rounded-full bg-muted px-2.5 py-0.5 text-xs font-medium">
            Full
          </span>
        ) : null}

        <span className="ml-auto text-xs text-muted-foreground">
          Updated {new Date(data.asOf).toLocaleTimeString()}
        </span>
      </div>

      <div
        className={
          compact
            ? 'grid grid-cols-2 gap-2'
            : 'grid gap-3 sm:grid-cols-2 lg:grid-cols-4'
        }
      >
        <Stat
          label="General capacity"
          value={String(data.generalCapacity)}
          hint="Active general spaces"
        />
        <Stat
          label="General used"
          value={String(data.generalUsed)}
          hint="Open general sessions"
          emphasis={data.isOverCapacity ? 'warning' : 'default'}
        />
        <Stat
          label="General free"
          value={String(data.generalFree)}
          hint={
            data.isGeneralPoolFull ? 'No room in the general pool' : undefined
          }
        />
        <Stat
          label="Reserved"
          value={`${data.reservedOccupied} / ${data.reservedSpaceCount}`}
          hint="Occupied / reserved spaces"
        />
      </div>

      <p className="mt-3 text-xs text-muted-foreground">
        Occupancy is lot-level and counted at the gate — not per-space sensed.
      </p>
    </div>
  )
}
