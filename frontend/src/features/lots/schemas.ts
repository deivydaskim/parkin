import { z } from 'zod'

export const accessModeSchema = z.enum(['Open', 'Restricted'])
export type AccessMode = z.infer<typeof accessModeSchema>
export const AccessMode = accessModeSchema.enum

export const fullBehaviorSchema = z.enum(['Block', 'AllowOverflow'])
export type FullBehavior = z.infer<typeof fullBehaviorSchema>
export const FullBehavior = fullBehaviorSchema.enum

export const lotStatusSchema = z.enum(['Active', 'Archived'])
export type LotStatus = z.infer<typeof lotStatusSchema>
export const LotStatus = lotStatusSchema.enum

export const lotLayoutSchema = z.object({
  widthMeters: z.number(),
  lengthMeters: z.number(),
  levelCount: z.number().int(),
})

export type LotLayout = z.infer<typeof lotLayoutSchema>

export const lotLayoutFormSchema = z.object({
  widthMeters: z
    .number({ error: 'Width must be a number' })
    .gt(0, 'Width must be greater than 0')
    .max(100_000, 'Width must be at most 100000 m'),
  lengthMeters: z
    .number({ error: 'Length must be a number' })
    .gt(0, 'Length must be greater than 0')
    .max(100_000, 'Length must be at most 100000 m'),
  levelCount: z
    .number({ error: 'Levels must be a number' })
    .int('Levels must be a whole number')
    .min(1, 'At least one level')
    .max(200, 'At most 200 levels'),
})

export const defaultLotLayout: LotLayout = {
  widthMeters: 50,
  lengthMeters: 30,
  levelCount: 1,
}

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
  layout: lotLayoutSchema.nullable(),
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
  hasLayout: z.boolean(),
  layout: lotLayoutFormSchema,
})

export type LotFormInput = z.infer<typeof lotFormSchema>

export const lotListParamsSchema = z.object({
  page: z.number().int().min(1).optional(),
  perPage: z.number().int().min(1).max(100).optional(),
  status: lotStatusSchema.or(z.literal('All')).optional(),
  search: z.string().optional(),
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
