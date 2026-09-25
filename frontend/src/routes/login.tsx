import { createFileRoute } from '@tanstack/react-router'
import { z } from 'zod'
import { BrandMark } from '@/components/BrandMark'
import { LoginForm } from '@/features/auth/components/LoginForm'
import { ParkingGridArt } from '@/features/auth/components/ParkingGridArt'

const loginSearchSchema = z.object({
  redirect: z.string().optional().catch(undefined),
})

export const Route = createFileRoute('/login')({
  validateSearch: loginSearchSchema,
  component: LoginPage,
})

function LoginPage() {
  const { redirect } = Route.useSearch()

  return (
    <div className="grid min-h-svh lg:grid-cols-[minmax(0,1.1fr)_minmax(0,1fr)]">
      <aside className="relative hidden overflow-hidden bg-sidebar text-sidebar-foreground lg:flex lg:flex-col lg:justify-between lg:p-12">
        <div className="bg-parking-grid absolute inset-0" aria-hidden />
        <div className="relative flex items-center gap-3">
          <BrandMark className="size-10 text-lg" />
          <span className="text-xl font-semibold tracking-tight text-sidebar-accent-foreground">
            Parkin
          </span>
        </div>
        <div className="relative space-y-6">
          <ParkingGridArt className="w-full max-w-md" />
          <div className="max-w-md space-y-2">
            <h2 className="text-3xl font-semibold tracking-tight text-balance text-sidebar-accent-foreground">
              Every lot, every plate, every gate — in one console.
            </h2>
            <p className="text-sidebar-foreground/70">
              Live occupancy, access decisions and reservations for your
              parking operation.
            </p>
          </div>
        </div>
        <p className="relative text-xs text-sidebar-foreground/50">
          Parking Management System
        </p>
      </aside>

      <main className="flex items-center justify-center p-6 sm:p-10">
        <div className="w-full max-w-sm space-y-8">
          <div className="flex items-center gap-3 lg:hidden">
            <BrandMark className="size-10 text-lg" />
            <span className="text-xl font-semibold tracking-tight">Parkin</span>
          </div>
          <LoginForm redirect={redirect ?? '/'} />
        </div>
      </main>
    </div>
  )
}
