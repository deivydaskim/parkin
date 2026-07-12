import { createFileRoute, Outlet, redirect } from '@tanstack/react-router'
import { AppNav } from '@/components/AppNav'
import { currentUserQueryOptions } from '@/features/auth/queries'
import { useAuthStore } from '@/features/auth/store'

export const Route = createFileRoute('/_authenticated')({
  beforeLoad: async ({ context, location }) => {
    try {
      const user = await context.queryClient.ensureQueryData(
        currentUserQueryOptions,
      )
      useAuthStore.getState().setUser(user)
    } catch {
      throw redirect({ to: '/login', search: { redirect: location.href } })
    }
  },
  component: AuthenticatedLayout,
})

function AuthenticatedLayout() {
  return (
    <div className="flex min-h-svh flex-col">
      <AppNav />
      <Outlet />
    </div>
  )
}
