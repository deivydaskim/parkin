import { ChevronRight } from 'lucide-react'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { StatusBadge } from '@/components/StatusBadge'
import { SpaceType, type Space } from '../schemas'

type Props = {
  spaces: Space[]
  onSelect: (space: Space) => void
}

function formatPosition(space: Space) {
  if (!space.placement) return 'Unplaced'
  const { x, y, rotationDegrees, level } = space.placement
  return `${x}, ${y} m · ${rotationDegrees}° · L${level}`
}

export function SpaceTable({ spaces, onSelect }: Props) {
  return (
    <div className="overflow-hidden rounded-xl border bg-card">
      <Table>
        <TableHeader>
          <TableRow className="bg-muted/40 hover:bg-muted/40">
            <TableHead>Label</TableHead>
            <TableHead>Type</TableHead>
            <TableHead>Status</TableHead>
            <TableHead className="hidden md:table-cell">Zone</TableHead>
            <TableHead className="hidden lg:table-cell">Position</TableHead>
            <TableHead>Holder</TableHead>
            <TableHead className="w-8" />
          </TableRow>
        </TableHeader>
        <TableBody>
          {spaces.map((space) => (
            <TableRow
              key={space.id}
              className="cursor-pointer"
              onClick={() => onSelect(space)}
            >
              <TableCell className="font-mono font-semibold">
                <button
                  type="button"
                  className="hover:underline focus-visible:underline focus-visible:outline-none"
                  onClick={(event) => {
                    event.stopPropagation()
                    onSelect(space)
                  }}
                >
                  {space.label}
                </button>
              </TableCell>
              <TableCell>
                <StatusBadge status={space.type} />
              </TableCell>
              <TableCell>
                <StatusBadge status={space.status} />
              </TableCell>
              <TableCell className="hidden text-muted-foreground md:table-cell">
                {space.zone ?? '—'}
              </TableCell>
              <TableCell className="hidden text-xs text-muted-foreground tabular-nums lg:table-cell">
                {formatPosition(space)}
              </TableCell>
              <TableCell>
                {space.type === SpaceType.Reserved ? (
                  (space.reservedDriverName ?? (
                    <span className="text-muted-foreground">Unassigned</span>
                  ))
                ) : (
                  <span className="text-muted-foreground">—</span>
                )}
              </TableCell>
              <TableCell>
                <ChevronRight className="size-4 text-muted-foreground" />
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  )
}
