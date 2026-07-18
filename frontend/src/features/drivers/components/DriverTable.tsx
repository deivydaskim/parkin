import { Link } from '@tanstack/react-router'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import type { Driver } from '../schemas'

type Props = {
  drivers: Driver[]
}

export function DriverTable({ drivers }: Props) {
  if (drivers.length === 0) {
    return <p className="text-sm text-muted-foreground">No drivers found.</p>
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Name</TableHead>
          <TableHead>Contact</TableHead>
          <TableHead>Plates</TableHead>
          <TableHead>Status</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {drivers.map((driver) => (
          <TableRow key={driver.id}>
            <TableCell className="font-medium">
              <Link
                to="/drivers/$driverId"
                params={{ driverId: driver.id }}
                className="hover:underline"
              >
                {driver.name}
              </Link>
            </TableCell>
            <TableCell>{driver.contact ?? '—'}</TableCell>
            <TableCell>{driver.plateCount}</TableCell>
            <TableCell>{driver.status}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
