import { useState } from 'react'
import { ChevronRight, Trash2 } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { ScrollArea } from '@/components/ui/scroll-area'
import { DENY_REASON_LABELS } from '@/lib/decision-messages'
import { cn } from '@/lib/utils'
import { useGateStore, type LogEntry } from '@/store/gate-store'

function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString([], { hour12: false })
}

function formatJson(value: unknown) {
  return typeof value === 'string' ? value : JSON.stringify(value, null, 2)
}

function requestText(entry: LogEntry) {
  const headers = Object.entries(entry.request.headers)
    .map(([name, value]) => `${name}: ${value}`)
    .join('\n')
  return `${entry.request.method} ${entry.request.path}\n${headers}\n\n${formatJson(entry.request.body)}`
}

function responseText(entry: LogEntry) {
  if (!entry.response) return entry.error ?? 'No response'
  return `HTTP ${entry.response.status}\n\n${formatJson(entry.response.body) || '(empty body)'}`
}

function DecisionChip({ entry }: { entry: LogEntry }) {
  if (!entry.decision) {
    return <Badge className="bg-warning text-warning-foreground">Offline</Badge>
  }
  const allowed = entry.decision.decision === 'Allow'
  return (
    <Badge
      className={cn(
        allowed
          ? 'bg-success text-success-foreground'
          : 'bg-destructive text-destructive-foreground',
      )}
    >
      {entry.decision.decision}
    </Badge>
  )
}

function Row({ entry }: { entry: LogEntry }) {
  const [open, setOpen] = useState(false)
  const reason = entry.decision?.reason
    ? DENY_REASON_LABELS[entry.decision.reason]
    : (entry.error ?? '')

  return (
    <li className="border-b last:border-b-0">
      <button
        type="button"
        onClick={() => setOpen((value) => !value)}
        className="flex w-full flex-wrap items-center gap-x-3 gap-y-1 px-3 py-2 text-left text-sm hover:bg-accent/40"
        aria-expanded={open}
      >
        <ChevronRight
          className={cn(
            'size-4 shrink-0 transition-transform',
            open && 'rotate-90',
          )}
        />
        <span className="font-mono text-xs text-muted-foreground">
          {formatTime(entry.at)}
        </span>
        <Badge variant="outline">{entry.lane}</Badge>
        <span className="font-mono font-semibold">{entry.plate}</span>
        <DecisionChip entry={entry} />
        {entry.replayOf && (
          <Badge variant="secondary">
            Replay · {entry.replayIdentical ? 'identical' : 'differs'}
          </Badge>
        )}
        <span className="text-xs text-muted-foreground">{reason}</span>
        <span className="ml-auto font-mono text-xs text-muted-foreground">
          {entry.latencyMs} ms
        </span>
      </button>
      {open && (
        <div className="grid gap-3 px-3 pb-3 lg:grid-cols-2">
          <div>
            <p className="mb-1 text-xs font-medium text-muted-foreground">
              Request
            </p>
            <pre className="overflow-x-auto rounded-md bg-black/40 p-3 font-mono text-xs leading-relaxed">
              {requestText(entry)}
            </pre>
          </div>
          <div>
            <p className="mb-1 text-xs font-medium text-muted-foreground">
              Response
            </p>
            <pre className="overflow-x-auto rounded-md bg-black/40 p-3 font-mono text-xs leading-relaxed">
              {responseText(entry)}
            </pre>
          </div>
        </div>
      )}
    </li>
  )
}

export function ApiLog() {
  const log = useGateStore((state) => state.log)
  const clearLog = useGateStore((state) => state.clearLog)

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle>API log</CardTitle>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={clearLog}
          disabled={log.length === 0}
        >
          <Trash2 />
          Clear
        </Button>
      </CardHeader>
      <CardContent className="px-0">
        {log.length === 0 ? (
          <p className="px-6 pb-2 text-sm text-muted-foreground">
            No events yet. Send a vehicle through a lane to see the exact
            request and response.
          </p>
        ) : (
          <ScrollArea className="h-96">
            <ul>
              {log.map((entry) => (
                <Row key={entry.id} entry={entry} />
              ))}
            </ul>
          </ScrollArea>
        )}
      </CardContent>
    </Card>
  )
}
