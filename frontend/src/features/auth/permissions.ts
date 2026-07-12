import { useAuthStore } from './store'
import type { CurrentUser, Role } from './schemas'

export function hasRole(user: CurrentUser | null, roles: Role[]): boolean {
  if (!user) return false
  return roles.some((role) => user.roles.includes(role))
}

export function useHasRole(...roles: Role[]): boolean {
  const user = useAuthStore((state) => state.user)
  return hasRole(user, roles)
}
