import { apiClient } from '@/lib/api-client'
import { reservationSchema, type Reservation } from './schemas'

export async function fetchActiveReservation(
  spaceId: string,
): Promise<Reservation | null> {
  const { data } = await apiClient.get(`/spaces/${spaceId}/reservation`)
  return data === null ? null : reservationSchema.parse(data)
}

export async function createReservation(
  spaceId: string,
  driverId: string,
): Promise<Reservation> {
  const { data } = await apiClient.post('/reservations', { spaceId, driverId })
  return reservationSchema.parse(data)
}

export async function cancelReservation(reservationId: string): Promise<Reservation> {
  const { data } = await apiClient.post(`/reservations/${reservationId}/cancel`)
  return reservationSchema.parse(data)
}
