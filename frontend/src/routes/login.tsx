import { createFileRoute } from '@tanstack/react-router'
import { z } from 'zod'
import { LoginForm } from '@/features/auth/components/LoginForm'

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
    <div className="flex min-h-svh items-center justify-center p-6">
      <LoginForm redirect={redirect ?? '/'} />
    </div>
  )
}
