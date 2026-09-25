import {
  queryOptions,
  useMutation,
  useQuery,
  useQueryClient,
  type QueryClient,
} from '@tanstack/react-query'
import { toast } from 'sonner'
import { qk } from '@/lib/query-keys'
import { parseApiError } from '@/lib/api-error'
import {
  cancelReservation,
  createReservation,
  fetchActiveReservation,
  reassignReservation,
} from './api'

export function activeReservationQueryOptions(
  spaceId: string,
  enabled: boolean,
) {
  return queryOptions({
    queryKey: qk.reservations.active(spaceId),
    queryFn: () => fetchActiveReservation(spaceId),
    enabled,
  })
}

export function useActiveReservation(spaceId: string, enabled: boolean) {
  return useQuery(activeReservationQueryOptions(spaceId, enabled))
}

function invalidateReservationViews(
  queryClient: QueryClient,
  lotId: string,
  spaceId: string,
) {
  queryClient.invalidateQueries({ queryKey: qk.reservations.active(spaceId) })
  queryClient.invalidateQueries({ queryKey: qk.spaces.list(lotId) })
  queryClient.invalidateQueries({ queryKey: qk.occupancy.lot(lotId) })
  queryClient.invalidateQueries({ queryKey: qk.lotLayout.detail(lotId) })
}

export function useCreateReservation(lotId: string, spaceId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (driverId: string) => createReservation(spaceId, driverId),
    onSuccess: () => {
      invalidateReservationViews(queryClient, lotId, spaceId)
      toast.success('Space reserved.')
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not create reservation.').message)
    },
  })
}

export function useReassignReservation(lotId: string, spaceId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (variables: { reservationId: string; driverId: string }) =>
      reassignReservation(variables.reservationId, variables.driverId),
    onSuccess: () => {
      invalidateReservationViews(queryClient, lotId, spaceId)
      toast.success('Reservation reassigned.')
    },
    onError: (error) => {
      toast.error(
        parseApiError(error, 'Could not reassign reservation.').message,
      )
    },
  })
}

export function useCancelReservation(lotId: string, spaceId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (reservationId: string) => cancelReservation(reservationId),
    onSuccess: () => {
      invalidateReservationViews(queryClient, lotId, spaceId)
      toast.success('Reservation cancelled.')
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not cancel reservation.').message)
    },
  })
}
