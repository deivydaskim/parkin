import { queryOptions, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { qk } from '@/lib/query-keys'
import { parseApiError } from '@/lib/api-error'
import {
  changeUserRole,
  createUser,
  disableUser,
  enableUser,
  fetchUsers,
} from './api'
import type { ChangeRoleInput, CreateUserInput, UserListParams } from './schemas'

export function usersQueryOptions(params?: UserListParams) {
  return queryOptions({
    queryKey: qk.users.list(params),
    queryFn: () => fetchUsers(params),
  })
}

export function useUsers(params?: UserListParams) {
  return useQuery(usersQueryOptions(params))
}

export function useCreateUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: CreateUserInput) => createUser(input),
    onSuccess: (user) => {
      queryClient.invalidateQueries({ queryKey: qk.users.list() })
      toast.success(`User "${user.displayName}" created.`)
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not create user.').message)
    },
  })
}

export function useDisableUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => disableUser(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: qk.users.list() })
      toast.success('User disabled.')
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not disable user.').message)
    },
  })
}

export function useEnableUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => enableUser(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: qk.users.list() })
      toast.success('User enabled.')
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not enable user.').message)
    },
  })
}

export function useChangeUserRole() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: ChangeRoleInput }) =>
      changeUserRole(id, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: qk.users.list() })
      toast.success('Role changed.')
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not change role.').message)
    },
  })
}
