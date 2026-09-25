import { useCallback } from 'react'
import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { Car, UserPlus } from 'lucide-react'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { DataTablePagination } from '@/components/DataTablePagination'
import { EmptyState } from '@/components/EmptyState'
import { PageHeader } from '@/components/PageHeader'
import { SearchInput } from '@/components/SearchInput'
import { ListSkeleton } from '@/components/Skeletons'
import { RoleGate } from '@/features/auth/components/RoleGate'
import { CreateDriverDialog } from '@/features/drivers/components/CreateDriverDialog'
import { DriverTable } from '@/features/drivers/components/DriverTable'
import { useDrivers } from '@/features/drivers/queries'
import { cn } from '@/lib/utils'

const statusOptions = ['Active', 'Archived', 'All'] as const

const driversSearchSchema = z.object({
  q: z.string().optional().catch(undefined),
  status: z.enum(statusOptions).optional().catch(undefined),
  page: z.number().int().min(1).optional().catch(undefined),
  perPage: z.number().int().min(1).max(100).optional().catch(undefined),
  create: z.boolean().optional().catch(undefined),
})

export const Route = createFileRoute('/_authenticated/drivers/')({
  validateSearch: driversSearchSchema,
  staticData: { crumb: 'Drivers' },
  component: DriversPage,
})

function DriversPage() {
  const {
    q = '',
    status = 'Active',
    page = 1,
    perPage = 20,
    create = false,
  } = Route.useSearch()
  const navigate = useNavigate({ from: Route.fullPath })
  const { data, isLoading, isPlaceholderData } = useDrivers({
    search: q,
    status,
    page,
    perPage,
  })
  const drivers = data?.items ?? []

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
        title="Drivers"
        description="People who park with you, their plates and the lots they can use."
        actions={
          <RoleGate roles={['Operator', 'SystemAdmin']}>
            <Button onClick={() => setCreateOpen(true)}>
              <UserPlus />
              New driver
            </Button>
          </RoleGate>
        }
      />

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <SearchInput
          value={q}
          onChange={setSearch}
          placeholder="Search name, contact or plate"
        />
        <Tabs
          value={status}
          onValueChange={(next) =>
            navigate({
              search: (prev) => ({
                ...prev,
                status:
                  next === 'Active'
                    ? undefined
                    : (next as (typeof statusOptions)[number]),
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
        <ListSkeleton rows={6} />
      ) : drivers.length === 0 ? (
        q || status !== 'Active' ? (
          <EmptyState
            icon={Car}
            title="No drivers match"
            hint="Search looks at names, contact details and plate numbers."
            action={
              <Button variant="outline" onClick={() => navigate({ search: {} })}>
                Clear filters
              </Button>
            }
          />
        ) : (
          <EmptyState
            icon={Car}
            title="No drivers yet"
            hint="Register a driver, then add their plates and grant lot access."
            action={
              <RoleGate roles={['Operator', 'SystemAdmin']}>
                <Button onClick={() => setCreateOpen(true)}>
                  <UserPlus />
                  Add your first driver
                </Button>
              </RoleGate>
            }
          />
        )
      ) : (
        <div className={cn('transition-opacity', isPlaceholderData && 'opacity-60')}>
          <DriverTable drivers={drivers} />
        </div>
      )}

      {data && data.totalCount > 0 ? (
        <DataTablePagination
          page={page}
          totalPages={data.totalPages}
          totalCount={data.totalCount}
          perPage={perPage}
          itemLabel="drivers"
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

      <CreateDriverDialog open={create} onOpenChange={setCreateOpen} />
    </div>
  )
}
