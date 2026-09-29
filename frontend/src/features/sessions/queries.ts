import { queryOptions, useQuery } from '@tanstack/react-query'
import { qk } from '@/lib/query-keys'
import { fetchActiveSessionsByLot } from './api'
import type { ActiveSessionListParams } from './schemas'

const ACTIVE_SESSIONS_REFETCH_INTERVAL_MS = 10_000

export function activeSessionsQueryOptions(lotId: string, params?: ActiveSessionListParams) {
  return queryOptions({
    queryKey: qk.sessions.activeByLot(lotId, params),
    queryFn: () => fetchActiveSessionsByLot(lotId, params),
    enabled: !!lotId,
    refetchInterval: ACTIVE_SESSIONS_REFETCH_INTERVAL_MS,
    refetchOnWindowFocus: true,
  })
}

export function useActiveSessions(lotId: string, params?: ActiveSessionListParams) {
  return useQuery(activeSessionsQueryOptions(lotId, params))
}
