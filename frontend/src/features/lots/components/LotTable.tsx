import { Link } from '@tanstack/react-router'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import type { Lot } from '../schemas'

type Props = {
  lots: Lot[]
}

const accessModeLabel: Record<Lot['accessMode'], string> = {
  Open: 'Open',
  Restricted: 'Restricted',
}

const fullBehaviorLabel: Record<Lot['fullBehavior'], string> = {
  Block: 'Block',
  AllowOverflow: 'Allow overflow',
}

const statusLabel: Record<Lot['status'], string> = {
  Active: 'Active',
  Archived: 'Archived',
}

export function LotTable({ lots }: Props) {
  if (lots.length === 0) {
    return <p className="text-sm text-muted-foreground">No lots found.</p>
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Name</TableHead>
          <TableHead>Timezone</TableHead>
          <TableHead>Access mode</TableHead>
          <TableHead>When full</TableHead>
          <TableHead>Capacity</TableHead>
          <TableHead>Status</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {lots.map((lot) => (
          <TableRow key={lot.id}>
            <TableCell className="font-medium">
              <Link
                to="/lots/$lotId"
                params={{ lotId: lot.id }}
                className="hover:underline"
              >
                {lot.name}
              </Link>
            </TableCell>
            <TableCell>{lot.timezone}</TableCell>
            <TableCell>{accessModeLabel[lot.accessMode]}</TableCell>
            <TableCell>{fullBehaviorLabel[lot.fullBehavior]}</TableCell>
            <TableCell>{lot.capacity}</TableCell>
            <TableCell>{statusLabel[lot.status]}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
