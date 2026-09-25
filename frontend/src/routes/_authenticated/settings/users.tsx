import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { Users } from 'lucide-react'
import { z } from 'zod'
import { DataTablePagination } from '@/components/DataTablePagination'
import { EmptyState } from '@/components/EmptyState'
import { PageHeader } from '@/components/PageHeader'
import { ListSkeleton } from '@/components/Skeletons'
import { CreateUserDialog } from '@/features/users/components/CreateUserDialog'
import { UserTable } from '@/features/users/components/UserTable'
import { useUsers } from '@/features/users/queries'

const PAGE_SIZE = 20

const usersSearchSchema = z.object({
  page: z.number().int().min(1).optional().catch(undefined),
})

export const Route = createFileRoute('/_authenticated/settings/users')({
  validateSearch: usersSearchSchema,
  staticData: {
    crumb: 'Staff',
    parentCrumbs: [{ label: 'Administration' }],
  },
  component: UsersPage,
})

function UsersPage() {
  const { page = 1 } = Route.useSearch()
  const navigate = useNavigate({ from: Route.fullPath })
  const { data, isLoading } = useUsers({ page, perPage: PAGE_SIZE })
  const users = data?.items ?? []

  return (
    <div className="space-y-6">
      <PageHeader
        title="Staff"
        description="Accounts that can sign in to this console. Operators run the lots; system admins also manage staff, keys and the audit log."
        actions={<CreateUserDialog />}
      />

      {isLoading ? (
        <ListSkeleton rows={4} />
      ) : users.length === 0 ? (
        <EmptyState icon={Users} title="No staff accounts" />
      ) : (
        <UserTable users={users} />
      )}

      {data && data.totalPages > 1 ? (
        <DataTablePagination
          page={page}
          totalPages={data.totalPages}
          totalCount={data.totalCount}
          perPage={PAGE_SIZE}
          itemLabel="staff"
          onPageChange={(next) => navigate({ search: { page: next } })}
        />
      ) : null}
    </div>
  )
}
