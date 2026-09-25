import { useState } from 'react'
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
import { StatusBadge } from '@/components/StatusBadge'
import { formatDate, formatDateTime } from '@/lib/format'
import { useRevokeApiKey } from '../queries'
import type { ApiKey } from '../schemas'

type Props = {
  apiKeys: ApiKey[]
}

export function ApiKeyTable({ apiKeys }: Props) {
  const [revoking, setRevoking] = useState<ApiKey | null>(null)
  const revokeMutation = useRevokeApiKey()

  return (
    <div className="overflow-hidden rounded-xl border bg-card">
      <Table>
        <TableHeader>
          <TableRow className="bg-muted/40 hover:bg-muted/40">
            <TableHead>Name</TableHead>
            <TableHead>Key</TableHead>
            <TableHead className="hidden sm:table-cell">Created</TableHead>
            <TableHead>Status</TableHead>
            <TableHead className="text-right">
              <span className="sr-only">Actions</span>
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {apiKeys.map((apiKey) => (
            <TableRow key={apiKey.id}>
              <TableCell className="font-medium">{apiKey.name}</TableCell>
              <TableCell>
                <code className="rounded bg-muted px-1.5 py-0.5 font-mono text-xs">
                  {apiKey.prefix}…
                </code>
              </TableCell>
              <TableCell
                className="hidden text-sm text-muted-foreground sm:table-cell"
                title={formatDateTime(apiKey.createdAt)}
              >
                {formatDate(apiKey.createdAt)}
              </TableCell>
              <TableCell>
                <StatusBadge
                  status={apiKey.status}
                  label={
                    apiKey.revokedAt
                      ? `Revoked ${formatDate(apiKey.revokedAt)}`
                      : undefined
                  }
                />
              </TableCell>
              <TableCell className="text-right">
                {apiKey.status === 'Active' ? (
                  <Button
                    variant="ghost"
                    size="sm"
                    className="text-destructive hover:bg-destructive/10 hover:text-destructive"
                    onClick={() => setRevoking(apiKey)}
                  >
                    Revoke
                  </Button>
                ) : null}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>

      <ConfirmDialog
        open={revoking !== null}
        onOpenChange={(open) => {
          if (!open) setRevoking(null)
        }}
        title={`Revoke "${revoking?.name}"?`}
        description="Any gate or integration using this key is rejected immediately. This cannot be undone."
        confirmLabel="Revoke key"
        pendingLabel="Revoking…"
        destructive
        isPending={revokeMutation.isPending}
        onConfirm={() => {
          if (revoking)
            revokeMutation.mutate(revoking.id, {
              onSuccess: () => setRevoking(null),
            })
        }}
      />
    </div>
  )
}
