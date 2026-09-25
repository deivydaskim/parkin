import { createFileRoute, Outlet, redirect } from '@tanstack/react-router'
import { hasRole } from '@/features/auth/permissions'
import { useAuthStore } from '@/features/auth/store'

export const Route = createFileRoute('/_authenticated/settings')({
  beforeLoad: () => {
    const user = useAuthStore.getState().user
    if (!hasRole(user, ['SystemAdmin'])) {
      throw redirect({ to: '/' })
    }
  },
  component: Outlet,
})
