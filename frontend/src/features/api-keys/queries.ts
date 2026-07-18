import { queryOptions, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { qk } from '@/lib/query-keys'
import { parseApiError } from '@/lib/api-error'
import { createApiKey, fetchApiKeys, revokeApiKey } from './api'
import type { CreateApiKeyInput } from './schemas'

export function apiKeysQueryOptions() {
  return queryOptions({
    queryKey: qk.apiKeys.list(),
    queryFn: fetchApiKeys,
  })
}

export function useApiKeys() {
  return useQuery(apiKeysQueryOptions())
}

export function useCreateApiKey() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: CreateApiKeyInput) => createApiKey(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: qk.apiKeys.list() })
      toast.success('API key created.')
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not create API key.').message)
    },
  })
}

export function useRevokeApiKey() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => revokeApiKey(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: qk.apiKeys.list() })
      toast.success('API key revoked.')
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not revoke API key.').message)
    },
  })
}
