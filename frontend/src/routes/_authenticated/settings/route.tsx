import { createFileRoute, Link, Outlet, redirect } from '@tanstack/react-router'
import { hasRole } from '@/features/auth/permissions'
import { useAuthStore } from '@/features/auth/store'

export const Route = createFileRoute('/_authenticated/settings')({
  beforeLoad: () => {
    const user = useAuthStore.getState().user
    if (!hasRole(user, ['SystemAdmin'])) {
      throw redirect({ to: '/' })
    }
  },
  component: SettingsLayout,
})

function SettingsLayout() {
  return (
    <div className="flex flex-1">
      <nav className="w-48 shrink-0 border-r p-4">
        <Link
          to="/settings/users"
          className="block text-sm font-medium hover:underline"
        >
          Users
        </Link>
        <Link
          to="/settings/api-keys"
          className="mt-2 block text-sm font-medium hover:underline"
        >
          API Keys
        </Link>
      </nav>
      <div className="flex-1">
        <Outlet />
      </div>
    </div>
  )
}
