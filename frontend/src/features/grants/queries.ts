import {
  queryOptions,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import { toast } from 'sonner'
import { qk } from '@/lib/query-keys'
import { parseApiError } from '@/lib/api-error'
import { createGrant, fetchGrantsByDriver, revokeGrant } from './api'
import type { Grant, GrantFormInput, GrantListParams } from './schemas'

export function grantsQueryOptions(driverId: string, params?: GrantListParams) {
  return queryOptions({
    queryKey: qk.grants.list(driverId, params),
    queryFn: () => fetchGrantsByDriver(driverId, params),
    enabled: !!driverId,
  })
}

export function useGrants(driverId: string, params?: GrantListParams) {
  return useQuery(grantsQueryOptions(driverId, params))
}

export function useCreateGrant(driverId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: GrantFormInput) => createGrant(driverId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: qk.grants.list(driverId) })
      toast.success('Access grant created.')
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not create grant.').message)
    },
  })
}

export function useRevokeGrant(driverId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (grantId: string) => revokeGrant(grantId),
    onSuccess: (grant: Grant) => {
      queryClient.invalidateQueries({ queryKey: qk.grants.list(driverId) })
      toast.success(`Grant revoked.`)
      return grant
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not revoke grant.').message)
    },
  })
}
