import {
  queryOptions,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import { toast } from 'sonner'
import { qk } from '@/lib/query-keys'
import { parseApiError } from '@/lib/api-error'
import { fetchCurrentUser, login, logout } from './api'
import { useAuthStore } from './store'

export const currentUserQueryOptions = queryOptions({
  queryKey: qk.auth.me(),
  queryFn: fetchCurrentUser,
  retry: false,
  staleTime: 5 * 60 * 1000,
})

export function useCurrentUser() {
  return useQuery(currentUserQueryOptions)
}

export function useLoginMutation() {
  const queryClient = useQueryClient()
  const setUser = useAuthStore((s) => s.setUser)

  return useMutation({
    mutationFn: login,
    onSuccess: (user) => {
      setUser(user)
      queryClient.setQueryData(qk.auth.me(), user)
    },
    onError: (error) => {
      const { message } = parseApiError(error)
      toast.error(message)
    },
  })
}

export function useLogoutMutation() {
  const queryClient = useQueryClient()
  const clear = useAuthStore((s) => s.clear)

  return useMutation({
    mutationFn: logout,
    onSuccess: () => {
      clear()
      queryClient.removeQueries({ queryKey: qk.auth.me() })
      toast.success('Signed out.')
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not sign out.').message)
    },
  })
}
