import { useHasRole } from '../permissions'

interface RoleGateProps {
  roles: string[]
  fallback?: React.ReactNode
  children: React.ReactNode
}

export function RoleGate({ roles, fallback = null, children }: RoleGateProps) {
  const hasRequiredRole = useHasRole(...roles)

  if (!hasRequiredRole) {
    return fallback
  }

  return children
}
