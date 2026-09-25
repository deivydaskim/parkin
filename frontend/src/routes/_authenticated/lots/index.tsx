import { useCallback } from 'react'
import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { ParkingSquare, Plus } from 'lucide-react'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { DataTablePagination } from '@/components/DataTablePagination'
import { EmptyState } from '@/components/EmptyState'
import { PageHeader } from '@/components/PageHeader'
import { SearchInput } from '@/components/SearchInput'
import { CardSkeleton } from '@/components/Skeletons'
import { RoleGate } from '@/features/auth/components/RoleGate'
import { CreateLotDialog } from '@/features/lots/components/CreateLotDialog'
import { LotCard } from '@/features/lots/components/LotCard'
import { useLots } from '@/features/lots/queries'

const PAGE_SIZE = 12

const statusOptions = ['Active', 'Archived', 'All'] as const

const lotsSearchSchema = z.object({
  q: z.string().optional().catch(undefined),
  status: z.enum(statusOptions).optional().catch(undefined),
  page: z.number().int().min(1).optional().catch(undefined),
  create: z.boolean().optional().catch(undefined),
})

export const Route = createFileRoute('/_authenticated/lots/')({
  validateSearch: lotsSearchSchema,
  staticData: { crumb: 'Parking lots' },
  component: LotsPage,
})

function LotsPage() {
  const { q = '', status = 'Active', page = 1, create = false } =
    Route.useSearch()
  const navigate = useNavigate({ from: Route.fullPath })
  const { data, isLoading, isPlaceholderData } = useLots({
    search: q,
    status,
    page,
    perPage: PAGE_SIZE,
  })
  const lots = data?.items ?? []

  const setSearch = useCallback(
    (next: string) =>
      navigate({
        search: (prev) => ({ ...prev, q: next || undefined, page: undefined }),
        replace: true,
      }),
    [navigate],
  )

  function setCreateOpen(open: boolean) {
    navigate({
      search: (prev) => ({ ...prev, create: open || undefined }),
      replace: true,
    })
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Parking lots"
        description="Every lot you operate, with its access rules and capacity."
        actions={
          <RoleGate roles={['Operator', 'SystemAdmin']}>
            <Button onClick={() => setCreateOpen(true)}>
              <Plus />
              New lot
            </Button>
          </RoleGate>
        }
      />

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <SearchInput
          value={q}
          onChange={setSearch}
          placeholder="Search by name or address"
        />
        <Tabs
          value={status}
          onValueChange={(next) =>
            navigate({
              search: (prev) => ({
                ...prev,
                status: next === 'Active' ? undefined : (next as (typeof statusOptions)[number]),
                page: undefined,
              }),
            })
          }
        >
          <TabsList>
            {statusOptions.map((option) => (
              <TabsTrigger key={option} value={option}>
                {option}
              </TabsTrigger>
            ))}
          </TabsList>
        </Tabs>
      </div>

      {isLoading ? (
        <CardSkeleton count={6} />
      ) : lots.length === 0 ? (
        q || status !== 'Active' ? (
          <EmptyState
            icon={ParkingSquare}
            title="No lots match"
            hint="Try a different search or status filter."
            action={
              <Button
                variant="outline"
                onClick={() => navigate({ search: {} })}
              >
                Clear filters
              </Button>
            }
          />
        ) : (
          <EmptyState
            icon={ParkingSquare}
            title="No parking lots yet"
            hint="A lot holds your spaces, access rules and live occupancy."
            action={
              <RoleGate roles={['Operator', 'SystemAdmin']}>
                <Button onClick={() => setCreateOpen(true)}>
                  <Plus />
                  Create your first lot
                </Button>
              </RoleGate>
            }
          />
        )
      ) : (
        <div
          className={
            isPlaceholderData
              ? 'grid gap-4 opacity-60 transition-opacity sm:grid-cols-2 xl:grid-cols-3'
              : 'grid gap-4 transition-opacity sm:grid-cols-2 xl:grid-cols-3'
          }
        >
          {lots.map((lot) => (
            <LotCard key={lot.id} lot={lot} />
          ))}
        </div>
      )}

      {data && data.totalCount > 0 ? (
        <DataTablePagination
          page={page}
          totalPages={data.totalPages}
          totalCount={data.totalCount}
          perPage={PAGE_SIZE}
          itemLabel="lots"
          onPageChange={(next) =>
            navigate({ search: (prev) => ({ ...prev, page: next }) })
          }
        />
      ) : null}

      <CreateLotDialog open={create} onOpenChange={setCreateOpen} />
    </div>
  )
}
