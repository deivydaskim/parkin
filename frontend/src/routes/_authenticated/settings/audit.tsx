import { useState } from 'react'
import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { ScrollText } from 'lucide-react'
import { z } from 'zod'
import { DataTablePagination } from '@/components/DataTablePagination'
import { EmptyState } from '@/components/EmptyState'
import { PageHeader } from '@/components/PageHeader'
import { ListSkeleton } from '@/components/Skeletons'
import { AuditEntrySheet } from '@/features/audit/components/AuditEntrySheet'
import { AuditFilters } from '@/features/audit/components/AuditFilters'
import { AuditTable } from '@/features/audit/components/AuditTable'
import { useAuditLog } from '@/features/audit/queries'
import {
  auditActorTypeSchema,
  auditEntityTypeSchema,
  type AuditFilters as AuditFiltersValue,
  type AuditLogEntry,
} from '@/features/audit/schemas'
import { cn } from '@/lib/utils'

const auditSearchSchema = z.object({
  page: z.number().int().min(1).optional().catch(undefined),
  perPage: z.number().int().min(1).max(100).optional().catch(undefined),
  from: z.string().optional().catch(undefined),
  to: z.string().optional().catch(undefined),
  actor: z.uuid().optional().catch(undefined),
  actorType: auditActorTypeSchema.optional().catch(undefined),
  entity: auditEntityTypeSchema.optional().catch(undefined),
})

export const Route = createFileRoute('/_authenticated/settings/audit')({
  validateSearch: auditSearchSchema,
  staticData: {
    crumb: 'Audit log',
    parentCrumbs: [{ label: 'Administration' }],
  },
  component: AuditPage,
})

function toRangeStart(date: string): string {
  return new Date(`${date}T00:00:00`).toISOString()
}

function toRangeEnd(date: string): string {
  return new Date(`${date}T23:59:59.999`).toISOString()
}

function AuditPage() {
  const search = Route.useSearch()
  const navigate = useNavigate({ from: Route.fullPath })
  const [selected, setSelected] = useState<AuditLogEntry | null>(null)
  const page = search.page ?? 1
  const perPage = search.perPage ?? 20

  const filters: AuditFiltersValue = {
    from: search.from ?? '',
    to: search.to ?? '',
    actor: search.actor ?? '',
    actorType: search.actorType ?? '',
    entity: search.entity ?? '',
  }

  const { data, isLoading, isPlaceholderData } = useAuditLog({
    page,
    perPage,
    from: search.from ? toRangeStart(search.from) : undefined,
    to: search.to ? toRangeEnd(search.to) : undefined,
    actor: search.actor,
    actorType: search.actorType,
    entity: search.entity,
  })
  const entries = data?.items ?? []

  function applyFilters(next: AuditFiltersValue) {
    navigate({
      search: (prev) => ({
        perPage: prev.perPage,
        from: next.from || undefined,
        to: next.to || undefined,
        actor: next.actor || undefined,
        actorType: next.actorType || undefined,
        entity: next.entity || undefined,
      }),
      replace: true,
    })
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Audit log"
        description="An append-only record of every access decision and every change made by staff, integrations and the system."
      />

      <AuditFilters value={filters} onChange={applyFilters} />

      {isLoading ? (
        <ListSkeleton rows={8} />
      ) : entries.length === 0 ? (
        <EmptyState
          icon={ScrollText}
          title="No matching entries"
          hint="Try widening the date range or clearing some filters."
        />
      ) : (
        <div className={cn('transition-opacity', isPlaceholderData && 'opacity-60')}>
          <AuditTable entries={entries} onSelect={setSelected} />
        </div>
      )}

      {data && data.totalCount > 0 ? (
        <DataTablePagination
          page={page}
          totalPages={data.totalPages}
          totalCount={data.totalCount}
          perPage={perPage}
          itemLabel="entries"
          onPageChange={(next) =>
            navigate({ search: (prev) => ({ ...prev, page: next }) })
          }
          onPerPageChange={(next) =>
            navigate({
              search: (prev) => ({ ...prev, perPage: next, page: undefined }),
            })
          }
        />
      ) : null}

      <AuditEntrySheet
        entry={selected}
        onOpenChange={(open) => {
          if (!open) setSelected(null)
        }}
      />
    </div>
  )
}
