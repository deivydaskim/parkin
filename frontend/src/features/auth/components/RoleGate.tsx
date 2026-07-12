import { useHasRole } from '../permissions'
import type { Role } from '../schemas'

type Props = {
  roles: Role[]
  fallback?: React.ReactNode
  children: React.ReactNode
}

export function RoleGate({ roles, fallback = null, children }: Props) {
  const hasRequiredRole = useHasRole(...roles)

  if (!hasRequiredRole) {
    return fallback
  }

  return children
}
