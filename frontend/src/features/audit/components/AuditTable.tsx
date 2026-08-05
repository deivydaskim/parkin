import { Fragment, useState } from 'react'
import { Button } from '@/components/ui/button'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import type { AuditLogEntry } from '../schemas'

type Props = {
  entries: AuditLogEntry[]
  page: number
  totalCount: number
  totalPages: number
  onPageChange: (page: number) => void
}

function formatMetadata(metadataJson: string | null): string | null {
  if (!metadataJson) return null
  try {
    return JSON.stringify(JSON.parse(metadataJson), null, 2)
  } catch {
    return metadataJson
  }
}

export function AuditTable({
  entries,
  page,
  totalCount,
  totalPages,
  onPageChange,
}: Props) {
  const [expandedId, setExpandedId] = useState<string | null>(null)

  if (entries.length === 0) {
    return (
      <p className="text-sm text-muted-foreground">No audit entries found.</p>
    )
  }

  return (
    <div className="space-y-3">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Timestamp</TableHead>
            <TableHead>Actor</TableHead>
            <TableHead>Action</TableHead>
            <TableHead>Entity</TableHead>
            <TableHead>Entity ID</TableHead>
            <TableHead />
          </TableRow>
        </TableHeader>
        <TableBody>
          {entries.map((entry) => {
            const metadata = formatMetadata(entry.metadataJson)
            const isExpanded = expandedId === entry.id

            return (
              <Fragment key={entry.id}>
                <TableRow>
                  <TableCell>
                    {new Date(entry.occurredAt).toLocaleString()}
                  </TableCell>
                  <TableCell>
                    {entry.actorType}
                    {entry.actorId ? (
                      <span className="ml-1 font-mono text-xs text-muted-foreground">
                        {entry.actorId}
                      </span>
                    ) : null}
                  </TableCell>
                  <TableCell className="font-mono text-xs">
                    {entry.action}
                  </TableCell>
                  <TableCell>{entry.entityType}</TableCell>
                  <TableCell className="font-mono text-xs">
                    {entry.entityId}
                  </TableCell>
                  <TableCell>
                    {metadata ? (
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        onClick={() =>
                          setExpandedId(isExpanded ? null : entry.id)
                        }
                      >
                        {isExpanded ? 'Hide' : 'Details'}
                      </Button>
                    ) : null}
                  </TableCell>
                </TableRow>
                {isExpanded && metadata ? (
                  <TableRow>
                    <TableCell colSpan={6}>
                      <pre className="overflow-x-auto rounded bg-muted p-2 text-xs">
                        {metadata}
                      </pre>
                    </TableCell>
                  </TableRow>
                ) : null}
              </Fragment>
            )
          })}
        </TableBody>
      </Table>

      <div className="flex items-center justify-between text-sm text-muted-foreground">
        <span>
          Page {page} of {Math.max(totalPages, 1)} ({totalCount} total)
        </span>
        <div className="flex gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={page <= 1}
            onClick={() => onPageChange(page - 1)}
          >
            Previous
          </Button>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={page >= totalPages}
            onClick={() => onPageChange(page + 1)}
          >
            Next
          </Button>
        </div>
      </div>
    </div>
  )
}
