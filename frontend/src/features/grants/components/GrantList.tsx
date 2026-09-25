import { useState } from 'react'
import { Link } from '@tanstack/react-router'
import { Button } from '@/components/ui/button'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { ConfirmDialog } from '@/components/ConfirmDialog'
import { StatusBadge, type BadgeStatus } from '@/components/StatusBadge'
import { RoleGate } from '@/features/auth/components/RoleGate'
import { formatDate } from '@/lib/format'
import { GrantStatus, type Grant } from '../schemas'

type Props = {
  grants: Grant[]
  onRevoke: (grantId: string, onDone: () => void) => void
  isRevoking?: boolean
  canEdit: boolean
}

function effectiveStatus(grant: Grant): BadgeStatus {
  if (grant.status === GrantStatus.Revoked) return 'Revoked'
  if (grant.validTo && new Date(grant.validTo) < new Date()) return 'Expired'
  return 'Active'
}

export function GrantList({ grants, onRevoke, isRevoking = false, canEdit }: Props) {
  const [revoking, setRevoking] = useState<Grant | null>(null)

  return (
    <div className="overflow-hidden rounded-xl border bg-card">
      <Table>
        <TableHeader>
          <TableRow className="bg-muted/40 hover:bg-muted/40">
            <TableHead>Lot</TableHead>
            <TableHead>Valid</TableHead>
            <TableHead>Status</TableHead>
            <TableHead className="text-right">
              <span className="sr-only">Actions</span>
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {grants.map((grant) => {
            const status = effectiveStatus(grant)
            return (
              <TableRow key={grant.id}>
                <TableCell className="font-medium">
                  <Link
                    to="/lots/$lotId"
                    params={{ lotId: grant.parkingLotId }}
                    className="hover:underline"
                  >
                    {grant.parkingLotName ?? 'Unknown lot'}
                  </Link>
                </TableCell>
                <TableCell className="text-sm text-muted-foreground tabular-nums">
                  {formatDate(grant.validFrom)} –{' '}
                  {grant.validTo ? formatDate(grant.validTo) : 'no end date'}
                </TableCell>
                <TableCell>
                  <StatusBadge status={status} />
                </TableCell>
                <TableCell className="text-right">
                  {canEdit && grant.status === GrantStatus.Active ? (
                    <RoleGate roles={['Operator', 'SystemAdmin']}>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="text-destructive hover:bg-destructive/10 hover:text-destructive"
                        onClick={() => setRevoking(grant)}
                      >
                        Revoke
                      </Button>
                    </RoleGate>
                  ) : null}
                </TableCell>
              </TableRow>
            )
          })}
        </TableBody>
      </Table>

      <ConfirmDialog
        open={revoking !== null}
        onOpenChange={(open) => {
          if (!open) setRevoking(null)
        }}
        title={`Revoke access to ${revoking?.parkingLotName ?? 'this lot'}?`}
        description="The driver will be denied at this lot's gate from now on, if the lot is restricted. This can't be undone — grant access again instead."
        confirmLabel="Revoke access"
        pendingLabel="Revoking…"
        destructive
        isPending={isRevoking}
        onConfirm={() => {
          if (revoking) onRevoke(revoking.id, () => setRevoking(null))
        }}
      />
    </div>
  )
}
