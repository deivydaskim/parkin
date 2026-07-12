import { create } from 'zustand'
import type { CurrentUser } from './schemas'

interface AuthState {
  user: CurrentUser | null
  setUser: (user: CurrentUser | null) => void
  clear: () => void
}

// Synchronous mirror of the /auth/me query for guards and RoleGate (T1.2).
// The TanStack Query cache remains the source of truth.
export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  setUser: (user) => set({ user }),
  clear: () => set({ user: null }),
}))
