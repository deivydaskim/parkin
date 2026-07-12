import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { Button } from '@/components/ui/button'
import { useCurrentUser, useLogoutMutation } from '@/features/auth/queries'

export const Route = createFileRoute('/_authenticated/')({
  component: DashboardPage,
})

function DashboardPage() {
  const navigate = useNavigate()
  const { data: user } = useCurrentUser()
  const logoutMutation = useLogoutMutation()

  async function handleLogout() {
    await logoutMutation.mutateAsync()
    navigate({ to: '/login' })
  }

  return (
    <div className="p-6">
      <h1 className="text-xl font-semibold">Dashboard</h1>
      {user && (
        <p className="text-muted-foreground mt-1 text-sm">
          Signed in as {user.displayName} ({user.roles.join(', ')})
        </p>
      )}
      <Button
        className="mt-4"
        onClick={handleLogout}
        disabled={logoutMutation.isPending}
      >
        Log out
      </Button>
    </div>
  )
}
