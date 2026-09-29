import { z } from 'zod'
import { poolSchema } from '@/features/gate/schemas'

export const activeSessionSchema = z.object({
  id: z.uuid(),
  plate: z.string(),
  driverId: z.uuid().nullable(),
  driverName: z.string().nullable(),
  pool: poolSchema,
  spaceId: z.uuid().nullable(),
  spaceLabel: z.string().nullable(),
  entryTime: z.iso.datetime({ offset: true }),
})

export type ActiveSession = z.infer<typeof activeSessionSchema>

export const activeSessionListParamsSchema = z.object({
  page: z.number().int().min(1).optional(),
  perPage: z.number().int().min(1).max(100).optional(),
})

export type ActiveSessionListParams = z.infer<typeof activeSessionListParamsSchema>

export const activeSessionListResponseSchema = z.object({
  items: z.array(activeSessionSchema),
  page: z.number(),
  perPage: z.number(),
  totalCount: z.number(),
  totalPages: z.number(),
})

export type ActiveSessionListResponse = z.infer<typeof activeSessionListResponseSchema>
