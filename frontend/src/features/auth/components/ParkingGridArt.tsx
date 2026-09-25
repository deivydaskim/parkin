import { cn } from '@/lib/utils'

type Props = {
  className?: string
}

type Bay = { column: number; row: number; kind: 'free' | 'taken' | 'reserved' }

const COLUMNS = 8
const BAY_WIDTH = 44
const BAY_HEIGHT = 70
const AISLE = 40

const bays: Bay[] = Array.from({ length: COLUMNS * 2 }, (_, index) => {
  const column = index % COLUMNS
  const row = Math.floor(index / COLUMNS)
  const pattern = (column * 3 + row * 5) % 7
  const kind = pattern === 0 ? 'reserved' : pattern < 4 ? 'taken' : 'free'
  return { column, row, kind }
})

const fillFor: Record<Bay['kind'], string> = {
  free: 'fill-success/25 stroke-success/60',
  taken: 'fill-sidebar-primary/70 stroke-sidebar-primary',
  reserved: 'fill-warning/70 stroke-warning',
}

export function ParkingGridArt({ className }: Props) {
  const width = COLUMNS * BAY_WIDTH
  const height = BAY_HEIGHT * 2 + AISLE

  return (
    <svg
      viewBox={`-4 -4 ${width + 8} ${height + 8}`}
      className={cn('h-auto', className)}
      role="img"
      aria-label="Illustration of a parking lot with free, occupied and reserved bays"
    >
      <rect
        x={-4}
        y={BAY_HEIGHT}
        width={width + 8}
        height={AISLE}
        className="fill-white/5"
      />
      <line
        x1={0}
        x2={width}
        y1={BAY_HEIGHT + AISLE / 2}
        y2={BAY_HEIGHT + AISLE / 2}
        className="stroke-white/25"
        strokeDasharray="10 8"
        strokeWidth={2}
      />
      {bays.map((bay) => {
        const x = bay.column * BAY_WIDTH
        const y = bay.row === 0 ? 0 : BAY_HEIGHT + AISLE
        return (
          <g key={`${bay.column}-${bay.row}`}>
            <rect
              x={x + 6}
              y={y + 6}
              width={BAY_WIDTH - 12}
              height={BAY_HEIGHT - 12}
              rx={6}
              strokeWidth={1.5}
              className={fillFor[bay.kind]}
            />
            <line
              x1={x}
              x2={x}
              y1={y}
              y2={y + BAY_HEIGHT}
              className="stroke-white/20"
              strokeWidth={2}
            />
          </g>
        )
      })}
      <line
        x1={width}
        x2={width}
        y1={0}
        y2={height}
        className="stroke-white/20"
        strokeWidth={2}
      />
    </svg>
  )
}
