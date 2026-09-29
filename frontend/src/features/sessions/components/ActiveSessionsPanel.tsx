import { useState } from 'react'
import { Link } from '@tanstack/react-router'
import { Car } from 'lucide-react'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { DataTablePagination } from '@/components/DataTablePagination'
import { EmptyState } from '@/components/EmptyState'
import { ListSkeleton } from '@/components/Skeletons'
import { StatusBadge } from '@/components/StatusBadge'
import { formatDateTime, formatElapsed } from '@/lib/format'
import { useActiveSessions } from '../queries'

const PAGE_SIZE = 20

type Props = {
  lotId: string
}

export function ActiveSessionsPanel({ lotId }: Props) {
  const [page, setPage] = useState(1)
  const { data, isLoading } = useActiveSessions(lotId, { page, perPage: PAGE_SIZE })
  const sessions = data?.items ?? []

  if (isLoading) return <ListSkeleton rows={4} />

  if (sessions.length === 0) {
    return (
      <EmptyState
        icon={Car}
        title="No vehicles parked"
        hint="Vehicles appear here once they enter and disappear when they exit."
      />
    )
  }

  return (
    <div className="space-y-4">
      <div className="overflow-hidden rounded-xl border bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-muted/40 hover:bg-muted/40">
              <TableHead>Plate</TableHead>
              <TableHead>Driver</TableHead>
              <TableHead>Pool</TableHead>
              <TableHead>Entered</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {sessions.map((session) => (
              <TableRow key={session.id}>
                <TableCell className="font-mono font-medium">{session.plate}</TableCell>
                <TableCell>
                  {session.driverId ? (
                    <Link
                      to="/drivers/$driverId"
                      params={{ driverId: session.driverId }}
                      className="font-medium hover:underline"
                    >
                      {session.driverName ?? 'Unknown driver'}
                    </Link>
                  ) : (
                    <span className="text-muted-foreground">Unknown plate</span>
                  )}
                </TableCell>
                <TableCell>
                  <div className="flex items-center gap-2">
                    <StatusBadge status={session.pool} />
                    {session.spaceLabel ? (
                      <span className="text-sm text-muted-foreground">
                        Space {session.spaceLabel}
                      </span>
                    ) : null}
                  </div>
                </TableCell>
                <TableCell className="text-sm tabular-nums">
                  {formatDateTime(session.entryTime)}
                  <span className="ml-2 text-muted-foreground">
                    {formatElapsed(session.entryTime)}
                  </span>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      {data && data.totalPages > 1 ? (
        <DataTablePagination
          page={page}
          totalPages={data.totalPages}
          totalCount={data.totalCount}
          perPage={PAGE_SIZE}
          itemLabel="vehicles"
          onPageChange={setPage}
        />
      ) : null}
    </div>
  )
}
