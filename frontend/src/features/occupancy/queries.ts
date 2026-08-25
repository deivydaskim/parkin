import { queryOptions, useQuery } from '@tanstack/react-query'
import { qk } from '@/lib/query-keys'
import { fetchLotOccupancy } from './api'

// Nothing pushes gate events to the SPA, so poll.
const OCCUPANCY_REFETCH_INTERVAL_MS = 10_000

export function lotOccupancyQueryOptions(lotId: string) {
  return queryOptions({
    queryKey: qk.occupancy.lot(lotId),
    queryFn: () => fetchLotOccupancy(lotId),
    enabled: !!lotId,
    refetchInterval: OCCUPANCY_REFETCH_INTERVAL_MS,
    refetchOnWindowFocus: true,
  })
}

export function useLotOccupancy(lotId: string) {
  return useQuery(lotOccupancyQueryOptions(lotId))
}
