import { z } from 'zod'

export const reservationStatusSchema = z.enum(['Active', 'Cancelled'])
export type ReservationStatus = z.infer<typeof reservationStatusSchema>
export const ReservationStatus = reservationStatusSchema.enum

// Full resource, as returned by the API.
export const reservationSchema = z.object({
  id: z.uuid(),
  spaceId: z.uuid(),
  driverId: z.uuid(),
  lotId: z.uuid(),
  status: reservationStatusSchema,
})

export type Reservation = z.infer<typeof reservationSchema>
