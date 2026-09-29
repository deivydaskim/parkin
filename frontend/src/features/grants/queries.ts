import {
  queryOptions,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import { toast } from 'sonner'
import { qk } from '@/lib/query-keys'
import { parseApiError } from '@/lib/api-error'
import { createGrant, fetchGrantsByDriver, fetchGrantsByLot, revokeGrant } from './api'
import type { CreateGrantInput, GrantListParams } from './schemas'

export function grantsQueryOptions(driverId: string, params?: GrantListParams) {
  return queryOptions({
    queryKey: qk.grants.byDriver(driverId, params),
    queryFn: () => fetchGrantsByDriver(driverId, params),
    enabled: !!driverId,
  })
}

export function useGrants(driverId: string, params?: GrantListParams) {
  return useQuery(grantsQueryOptions(driverId, params))
}

export function grantsByLotQueryOptions(lotId: string, params?: GrantListParams) {
  return queryOptions({
    queryKey: qk.grants.byLot(lotId, params),
    queryFn: () => fetchGrantsByLot(lotId, params),
    enabled: !!lotId,
  })
}

export function useGrantsByLot(lotId: string, params?: GrantListParams) {
  return useQuery(grantsByLotQueryOptions(lotId, params))
}

export function useCreateGrant() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: CreateGrantInput) => createGrant(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: qk.grants.all() })
      toast.success('Access grant created.')
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not create grant.').message)
    },
  })
}

export function useRevokeGrant() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (grantId: string) => revokeGrant(grantId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: qk.grants.all() })
      toast.success('Grant revoked.')
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not revoke grant.').message)
    },
  })
}
