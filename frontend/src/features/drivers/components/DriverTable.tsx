import { Link, useNavigate } from '@tanstack/react-router'
import { ChevronRight } from 'lucide-react'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { StatusBadge } from '@/components/StatusBadge'
import { initialsOf } from '@/lib/format'
import type { Driver } from '../schemas'

type Props = {
  drivers: Driver[]
}

export function DriverTable({ drivers }: Props) {
  const navigate = useNavigate()

  return (
    <div className="overflow-hidden rounded-xl border bg-card">
      <Table>
        <TableHeader>
          <TableRow className="bg-muted/40 hover:bg-muted/40">
            <TableHead>Driver</TableHead>
            <TableHead className="hidden sm:table-cell">Contact</TableHead>
            <TableHead>Plates</TableHead>
            <TableHead>Status</TableHead>
            <TableHead className="w-8" />
          </TableRow>
        </TableHeader>
        <TableBody>
          {drivers.map((driver) => (
            <TableRow
              key={driver.id}
              className="cursor-pointer"
              onClick={() =>
                navigate({
                  to: '/drivers/$driverId',
                  params: { driverId: driver.id },
                })
              }
            >
              <TableCell>
                <div className="flex items-center gap-3">
                  <Avatar className="size-8">
                    <AvatarFallback className="bg-primary/10 text-xs font-semibold text-primary">
                      {initialsOf(driver.name)}
                    </AvatarFallback>
                  </Avatar>
                  <Link
                    to="/drivers/$driverId"
                    params={{ driverId: driver.id }}
                    className="font-medium hover:underline"
                    onClick={(event) => event.stopPropagation()}
                  >
                    {driver.name}
                  </Link>
                </div>
              </TableCell>
              <TableCell className="hidden text-muted-foreground sm:table-cell">
                {driver.contact ?? '—'}
              </TableCell>
              <TableCell>
                <Badge variant="secondary" className="tabular-nums">
                  {driver.plateCount} plate{driver.plateCount === 1 ? '' : 's'}
                </Badge>
              </TableCell>
              <TableCell>
                <StatusBadge status={driver.status} />
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
