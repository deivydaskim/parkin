import { z } from 'zod'

export const accessModeSchema = z.enum(['OPEN', 'RESTRICTED'])
export type AccessMode = z.infer<typeof accessModeSchema>

export const fullBehaviorSchema = z.enum(['BLOCK', 'ALLOW_OVERFLOW'])
export type FullBehavior = z.infer<typeof fullBehaviorSchema>

export const lotStatusSchema = z.enum(['ACTIVE', 'ARCHIVED'])
export type LotStatus = z.infer<typeof lotStatusSchema>

// Full resource, as returned by the API.
export const lotSchema = z.object({
  id: z.uuid(),
  name: z.string(),
  address: z.string().nullable(),
  timezone: z.string(),
  accessMode: accessModeSchema,
  fullBehavior: fullBehaviorSchema,
  status: lotStatusSchema,
  capacity: z.number().int().nonnegative(),
})

export type Lot = z.infer<typeof lotSchema>

// Shared by the create dialog and the edit/detail view (edit pre-fills from the fetched resource).
export const lotFormSchema = z.object({
  name: z
    .string()
    .min(1, 'Name is required')
    .max(200, 'Name must not exceed 200 characters'),
  address: z
    .string()
    .max(300, 'Address must not exceed 300 characters')
    .optional(),
  timezone: z.string().min(1, 'Timezone is required'),
  accessMode: accessModeSchema,
  fullBehavior: fullBehaviorSchema,
})

export type LotFormInput = z.infer<typeof lotFormSchema>

export const lotListParamsSchema = z.object({
  page: z.number().int().min(1).optional(),
  perPage: z.number().int().min(1).max(100).optional(),
  status: lotStatusSchema.or(z.literal('ALL')).optional(),
})

export type LotListParams = z.infer<typeof lotListParamsSchema>

export const lotListResponseSchema = z.object({
  items: z.array(lotSchema),
  page: z.number(),
  perPage: z.number(),
  totalCount: z.number(),
  totalPages: z.number(),
})

export type LotListResponse = z.infer<typeof lotListResponseSchema>
