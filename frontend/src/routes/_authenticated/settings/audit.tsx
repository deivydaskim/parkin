import { useState } from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { AuditFilters } from '@/features/audit/components/AuditFilters'
import { AuditTable } from '@/features/audit/components/AuditTable'
import { useAuditLog } from '@/features/audit/queries'
import type { AuditFilters as AuditFiltersInput } from '@/features/audit/schemas'

export const Route = createFileRoute('/_authenticated/settings/audit')({
  component: AuditPage,
})

const emptyFilters: AuditFiltersInput = {
  from: '',
  to: '',
  actor: '',
  actorType: '',
  entity: '',
}

// <input type="date"> only carries a calendar day; widen it to the day's
// start/end instants (local time) so `from`/`to` behave inclusively.
function toRangeStart(date: string): string {
  return new Date(`${date}T00:00:00`).toISOString()
}

function toRangeEnd(date: string): string {
  return new Date(`${date}T23:59:59.999`).toISOString()
}

function AuditPage() {
  const [page, setPage] = useState(1)
  const [filters, setFilters] = useState<AuditFiltersInput>(emptyFilters)

  const { data, isLoading } = useAuditLog({
    page,
    perPage: 20,
    from: filters.from ? toRangeStart(filters.from) : undefined,
    to: filters.to ? toRangeEnd(filters.to) : undefined,
    actor: filters.actor || undefined,
    actorType: filters.actorType || undefined,
    entity: filters.entity || undefined,
  })

  function handleApply(next: AuditFiltersInput) {
    setFilters(next)
    setPage(1)
  }

  function handleClear() {
    setFilters(emptyFilters)
    setPage(1)
  }

  return (
    <div className="p-6">
      <h1 className="mb-4 text-xl font-semibold">Audit log</h1>

      <div className="mb-6">
        <AuditFilters
          defaultValues={filters}
          onApply={handleApply}
          onClear={handleClear}
        />
      </div>

      {isLoading ? (
        <p className="text-sm text-muted-foreground">Loading…</p>
      ) : (
        <AuditTable
          entries={data?.items ?? []}
          page={data?.page ?? page}
          totalCount={data?.totalCount ?? 0}
          totalPages={data?.totalPages ?? 1}
          onPageChange={setPage}
        />
      )}
    </div>
  )
}
