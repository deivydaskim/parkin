import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { formatDateTime } from '@/lib/format'
import {
  actorDisplayName,
  actorTypeLabels,
  entityTypeLabel,
  humanizeAction,
} from '../labels'
import type { AuditLogEntry } from '../schemas'

type Props = {
  entry: AuditLogEntry | null
  onOpenChange: (open: boolean) => void
}

function parseMetadata(metadataJson: string | null) {
  if (!metadataJson) return null
  try {
    const parsed: unknown = JSON.parse(metadataJson)
    return {
      pretty: JSON.stringify(parsed, null, 2),
      fields:
        parsed && typeof parsed === 'object' && !Array.isArray(parsed)
          ? Object.entries(parsed as Record<string, unknown>)
          : [],
    }
  } catch {
    return { pretty: metadataJson, fields: [] }
  }
}

function formatValue(value: unknown) {
  if (value === null || value === undefined) return '—'
  if (typeof value === 'object') return JSON.stringify(value)
  return String(value)
}

function humanizeKey(key: string) {
  return key
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .replaceAll('_', ' ')
    .replace(/^./, (first) => first.toUpperCase())
}

export function AuditEntrySheet({ entry, onOpenChange }: Props) {
  const metadata = parseMetadata(entry?.metadataJson ?? null)

  return (
    <Sheet open={!!entry} onOpenChange={onOpenChange}>
      <SheetContent className="w-full gap-0 overflow-y-auto sm:max-w-md">
        {entry ? (
          <>
            <SheetHeader className="border-b">
              <SheetTitle>{humanizeAction(entry.action)}</SheetTitle>
              <SheetDescription>{formatDateTime(entry.occurredAt)}</SheetDescription>
            </SheetHeader>
            <div className="space-y-6 p-4">
              <dl className="grid grid-cols-[auto_minmax(0,1fr)] gap-x-4 gap-y-2 text-sm">
                <dt className="text-muted-foreground">Actor</dt>
                <dd>
                  {actorDisplayName(entry)}{' '}
                  <span className="text-muted-foreground">
                    ({actorTypeLabels[entry.actorType]})
                  </span>
                </dd>
                {entry.actorId ? (
                  <>
                    <dt className="text-muted-foreground">Actor ID</dt>
                    <dd className="font-mono text-xs break-all">{entry.actorId}</dd>
                  </>
                ) : null}
                <dt className="text-muted-foreground">Record</dt>
                <dd>{entityTypeLabel(entry.entityType)}</dd>
                <dt className="text-muted-foreground">Record ID</dt>
                <dd className="font-mono text-xs break-all">{entry.entityId}</dd>
                <dt className="text-muted-foreground">Action code</dt>
                <dd className="font-mono text-xs">{entry.action}</dd>
              </dl>

              {metadata ? (
                <>
                  {metadata.fields.length > 0 ? (
                    <section className="space-y-2">
                      <h3 className="text-sm font-semibold">Details</h3>
                      <dl className="divide-y rounded-lg border text-sm">
                        {metadata.fields.map(([key, value]) => (
                          <div
                            key={key}
                            className="grid grid-cols-[minmax(0,2fr)_minmax(0,3fr)] gap-3 px-3 py-2"
                          >
                            <dt className="text-muted-foreground">{humanizeKey(key)}</dt>
                            <dd className="font-medium break-words">{formatValue(value)}</dd>
                          </div>
                        ))}
                      </dl>
                    </section>
                  ) : null}
                  <section className="space-y-2">
                    <h3 className="text-sm font-semibold">Raw JSON</h3>
                    <pre className="overflow-x-auto rounded-lg bg-muted p-3 text-xs">
                      {metadata.pretty}
                    </pre>
                  </section>
                </>
              ) : (
                <p className="text-sm text-muted-foreground">
                  No additional details were recorded.
                </p>
              )}
            </div>
          </>
        ) : null}
      </SheetContent>
    </Sheet>
  )
}
