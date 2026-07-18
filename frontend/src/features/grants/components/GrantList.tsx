import { Button } from '@/components/ui/button'
import { RoleGate } from '@/features/auth/components/RoleGate'
import { useLots } from '@/features/lots/queries'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { GrantStatus, type Grant } from '../schemas'

type Props = {
  grants: Grant[]
  onRevoke: (grantId: string) => void
  isRevoking?: boolean
}

export function GrantList({ grants, onRevoke, isRevoking = false }: Props) {
  const { data: lotsData } = useLots({ perPage: 100 })
  const lotNameById = new Map(
    (lotsData?.items ?? []).map((lot) => [lot.id, lot.name]),
  )

  if (grants.length === 0) {
    return <p className="text-sm text-muted-foreground">No grants yet.</p>
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Lot</TableHead>
          <TableHead>Valid from</TableHead>
          <TableHead>Valid to</TableHead>
          <TableHead>Status</TableHead>
          <TableHead className="text-right">Actions</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {grants.map((grant) => (
          <TableRow key={grant.id}>
            <TableCell className="font-medium">
              {lotNameById.get(grant.parkingLotId) ?? grant.parkingLotId}
            </TableCell>
            <TableCell>
              {new Date(grant.validFrom).toLocaleDateString()}
            </TableCell>
            <TableCell>
              {grant.validTo
                ? new Date(grant.validTo).toLocaleDateString()
                : '—'}
            </TableCell>
            <TableCell>{grant.status}</TableCell>
            <TableCell className="text-right">
              {grant.status === GrantStatus.Active && (
                <RoleGate roles={['Operator', 'SystemAdmin']}>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => onRevoke(grant.id)}
                    disabled={isRevoking}
                  >
                    {isRevoking ? 'Revoking…' : 'Revoke'}
                  </Button>
                </RoleGate>
              )}
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
