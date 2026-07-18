import { z } from 'zod'

export const spaceTypeSchema = z.enum(['General', 'Reserved'])
export type SpaceType = z.infer<typeof spaceTypeSchema>
export const SpaceType = spaceTypeSchema.enum

export const spaceStatusSchema = z.enum(['Active', 'Inactive'])
export type SpaceStatus = z.infer<typeof spaceStatusSchema>
export const SpaceStatus = spaceStatusSchema.enum

// Full resource, as returned by the API.
export const spaceSchema = z.object({
  id: z.uuid(),
  lotId: z.uuid(),
  label: z.string(),
  type: spaceTypeSchema,
  status: spaceStatusSchema,
})

export type Space = z.infer<typeof spaceSchema>

export const spaceFormSchema = z.object({
  label: z
    .string()
    .min(1, 'Label is required')
    .max(100, 'Label must not exceed 100 characters'),
  type: spaceTypeSchema,
})

export type SpaceFormInput = z.infer<typeof spaceFormSchema>

export const spaceListParamsSchema = z.object({
  page: z.number().int().min(1).optional(),
  perPage: z.number().int().min(1).max(100).optional(),
  status: spaceStatusSchema.or(z.literal('All')).optional(),
})

export type SpaceListParams = z.infer<typeof spaceListParamsSchema>

export const spaceListResponseSchema = z.object({
  items: z.array(spaceSchema),
  page: z.number(),
  perPage: z.number(),
  totalCount: z.number(),
  totalPages: z.number(),
})

export type SpaceListResponse = z.infer<typeof spaceListResponseSchema>
