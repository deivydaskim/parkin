import { ChevronRight, KeyRound, Server, UserRound } from 'lucide-react'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { formatDateTime } from '@/lib/format'
import { actorDisplayName, entityTypeLabel, humanizeAction } from '../labels'
import type { AuditLogEntry } from '../schemas'

type Props = {
  entries: AuditLogEntry[]
  onSelect: (entry: AuditLogEntry) => void
}

const actorIcons = {
  Staff: UserRound,
  System: Server,
  Api: KeyRound,
} as const

export function AuditTable({ entries, onSelect }: Props) {
  return (
    <div className="overflow-hidden rounded-xl border bg-card">
      <Table>
        <TableHeader>
          <TableRow className="bg-muted/40 hover:bg-muted/40">
            <TableHead>When</TableHead>
            <TableHead>Who</TableHead>
            <TableHead>What happened</TableHead>
            <TableHead className="hidden md:table-cell">Record</TableHead>
            <TableHead className="w-8" />
          </TableRow>
        </TableHeader>
        <TableBody>
          {entries.map((entry) => {
            const ActorIcon = actorIcons[entry.actorType]
            return (
              <TableRow
                key={entry.id}
                className="cursor-pointer"
                onClick={() => onSelect(entry)}
              >
                <TableCell className="text-sm whitespace-nowrap text-muted-foreground tabular-nums">
                  {formatDateTime(entry.occurredAt)}
                </TableCell>
                <TableCell>
                  <span className="flex items-center gap-2">
                    <ActorIcon className="size-3.5 shrink-0 text-muted-foreground" aria-hidden />
                    <span className="truncate">{actorDisplayName(entry)}</span>
                  </span>
                </TableCell>
                <TableCell className="font-medium">
                  <button
                    type="button"
                    className="text-left hover:underline focus-visible:underline focus-visible:outline-none"
                    onClick={(event) => {
                      event.stopPropagation()
                      onSelect(entry)
                    }}
                  >
                    {humanizeAction(entry.action)}
                  </button>
                </TableCell>
                <TableCell className="hidden text-sm text-muted-foreground md:table-cell">
                  {entityTypeLabel(entry.entityType)}{' '}
                  <span className="font-mono text-xs">
                    {entry.entityId.slice(0, 8)}
                  </span>
                </TableCell>
                <TableCell>
                  <ChevronRight className="size-4 text-muted-foreground" />
                </TableCell>
              </TableRow>
            )
          })}
        </TableBody>
      </Table>
    </div>
  )
}
