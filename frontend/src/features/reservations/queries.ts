import {
  queryOptions,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import { toast } from 'sonner'
import { qk } from '@/lib/query-keys'
import { parseApiError } from '@/lib/api-error'
import { cancelReservation, createReservation, fetchActiveReservation } from './api'
import type { Reservation } from './schemas'

export function activeReservationQueryOptions(spaceId: string, enabled: boolean) {
  return queryOptions({
    queryKey: qk.reservations.active(spaceId),
    queryFn: () => fetchActiveReservation(spaceId),
    enabled,
  })
}

export function useActiveReservation(spaceId: string, enabled: boolean) {
  return useQuery(activeReservationQueryOptions(spaceId, enabled))
}

export function useCreateReservation(lotId: string, spaceId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (driverId: string) => createReservation(spaceId, driverId),
    onSuccess: (reservation: Reservation) => {
      queryClient.invalidateQueries({ queryKey: qk.reservations.active(spaceId) })
      queryClient.invalidateQueries({ queryKey: qk.spaces.list(lotId) })
      toast.success('Space reserved.')
      return reservation
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not create reservation.').message)
    },
  })
}

export function useCancelReservation(lotId: string, spaceId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (reservationId: string) => cancelReservation(reservationId),
    onSuccess: (reservation: Reservation) => {
      queryClient.invalidateQueries({ queryKey: qk.reservations.active(spaceId) })
      queryClient.invalidateQueries({ queryKey: qk.spaces.list(lotId) })
      toast.success('Reservation cancelled.')
      return reservation
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not cancel reservation.').message)
    },
  })
}
