import { createFileRoute } from '@tanstack/react-router'
import { CreateUserDialog } from '@/features/users/components/CreateUserDialog'
import { UserTable } from '@/features/users/components/UserTable'
import { useUsers } from '@/features/users/queries'

export const Route = createFileRoute('/_authenticated/settings/users')({
  component: UsersPage,
})

function UsersPage() {
  const { data, isLoading } = useUsers()

  return (
    <div className="p-6">
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-xl font-semibold">Staff accounts</h1>
        <CreateUserDialog />
      </div>

      {isLoading ? (
        <p className="text-sm text-muted-foreground">Loading…</p>
      ) : (
        <UserTable users={data?.items ?? []} />
      )}
    </div>
  )
}
