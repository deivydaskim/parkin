import { apiClient } from '@/lib/api-client'
import { reservationSchema, type Reservation } from './schemas'

export async function fetchActiveReservation(
  spaceId: string,
): Promise<Reservation | null> {

  const response = await apiClient.get(`/spaces/${spaceId}/reservation`, {
    validateStatus: (status) => status === 200 || status === 204,
  })
  return response.status === 204 ? null : reservationSchema.parse(response.data)
}

export async function createReservation(
  spaceId: string,
  driverId: string,
): Promise<Reservation> {
  const { data } = await apiClient.post('/reservations', { spaceId, driverId })
  return reservationSchema.parse(data)
}

export async function cancelReservation(
  reservationId: string,
): Promise<Reservation> {
  const { data } = await apiClient.post(`/reservations/${reservationId}/cancel`)
  return reservationSchema.parse(data)
}
