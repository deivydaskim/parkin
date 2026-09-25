import { useRef } from 'react'
import {
  queryOptions,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import { toast } from 'sonner'
import { qk } from '@/lib/query-keys'
import { parseApiError } from '@/lib/api-error'
import { fetchAccessEvents, recordManualEvent } from './api'
import type { AccessEventListParams, ManualEventFormInput } from './schemas'

type PendingAttempt = {
  signature: string
  idempotencyKey: string
}

const ACCESS_EVENTS_REFETCH_INTERVAL_MS = 15_000

export function accessEventsQueryOptions(
  lotId: string,
  params?: AccessEventListParams,
) {
  return queryOptions({
    queryKey: qk.accessEvents.list(lotId, params),
    queryFn: () => fetchAccessEvents(lotId, params),
    enabled: !!lotId,
    refetchInterval: ACCESS_EVENTS_REFETCH_INTERVAL_MS,
  })
}

export function useAccessEvents(lotId: string, params?: AccessEventListParams) {
  return useQuery(accessEventsQueryOptions(lotId, params))
}

export function useManualEvent(lotId: string) {
  const queryClient = useQueryClient()
  const pendingAttempt = useRef<PendingAttempt | null>(null)

  function idempotencyKeyFor(input: ManualEventFormInput) {
    const signature = JSON.stringify(input)
    if (pendingAttempt.current?.signature !== signature) {
      pendingAttempt.current = { signature, idempotencyKey: crypto.randomUUID() }
    }
    return pendingAttempt.current.idempotencyKey
  }

  return useMutation({
    mutationFn: (input: ManualEventFormInput) =>
      recordManualEvent(lotId, input, idempotencyKeyFor(input)),
    onSuccess: () => {
      pendingAttempt.current = null
      queryClient.invalidateQueries({ queryKey: qk.occupancy.lot(lotId) })
      queryClient.invalidateQueries({ queryKey: qk.occupancy.all() })
      queryClient.invalidateQueries({ queryKey: qk.accessEvents.list(lotId) })
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not record the event.').message)
    },
  })
}
