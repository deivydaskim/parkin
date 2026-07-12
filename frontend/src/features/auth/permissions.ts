import { useAuthStore } from './store'
import type { CurrentUser } from './schemas'

export function hasRole(user: CurrentUser | null, roles: string[]): boolean {
  if (!user) return false
  return roles.some((role) => user.roles.includes(role))
}

export function useHasRole(...roles: string[]): boolean {
  const user = useAuthStore((state) => state.user)
  return hasRole(user, roles)
}
