import { z } from 'zod'

export const spaceTypeSchema = z.enum(['General', 'Reserved'])
export type SpaceType = z.infer<typeof spaceTypeSchema>
export const SpaceType = spaceTypeSchema.enum

export const spaceStatusSchema = z.enum(['Active', 'Inactive'])
export type SpaceStatus = z.infer<typeof spaceStatusSchema>
export const SpaceStatus = spaceStatusSchema.enum

export const DEFAULT_BAY_WIDTH = 2.5
export const DEFAULT_BAY_LENGTH = 5
export const ZONE_MAX_LENGTH = 50

export const spacePlacementSchema = z.object({
  x: z.number(),
  y: z.number(),
  rotationDegrees: z.number(),
  level: z.number().int(),
  width: z.number(),
  length: z.number(),
})

export type SpacePlacement = z.infer<typeof spacePlacementSchema>

// Full resource, as returned by the API.
export const spaceSchema = z.object({
  id: z.uuid(),
  lotId: z.uuid(),
  label: z.string(),
  type: spaceTypeSchema,
  status: spaceStatusSchema,
  zone: z.string().nullable(),
  placement: spacePlacementSchema.nullable(),
})

export type Space = z.infer<typeof spaceSchema>

const metres = (label: string, max: number) =>
  z
    .number({ error: `${label} must be a number` })
    .min(0, `${label} must be 0 or more`)
    .max(max, `${label} must be at most ${max}`)

export const placementFormSchema = z.object({
  x: metres('X', 100_000),
  y: metres('Y', 100_000),
  rotationDegrees: z
    .number({ error: 'Rotation must be a number' })
    .min(0, 'Rotation must be between 0 and 359.99')
    .lt(360, 'Rotation must be between 0 and 359.99'),
  level: z
    .number({ error: 'Level must be a number' })
    .int('Level must be a whole number')
    .min(0, 'Level must be 0 or more')
    .max(200, 'Level must be at most 200'),
  width: metres('Width', 50).gt(0, 'Width must be greater than 0'),
  length: metres('Length', 50).gt(0, 'Length must be greater than 0'),
})

export type PlacementFormInput = z.infer<typeof placementFormSchema>

export const defaultPlacement: PlacementFormInput = {
  x: 0,
  y: 0,
  rotationDegrees: 0,
  level: 0,
  width: DEFAULT_BAY_WIDTH,
  length: DEFAULT_BAY_LENGTH,
}

export const spaceFormSchema = z.object({
  label: z
    .string()
    .min(1, 'Label is required')
    .max(100, 'Label must not exceed 100 characters'),
  type: spaceTypeSchema,
  zone: z
    .string()
    .max(ZONE_MAX_LENGTH, `Zone must not exceed ${ZONE_MAX_LENGTH} characters`)
    .optional(),
  isPlaced: z.boolean(),
  placement: placementFormSchema,
})

export type SpaceFormInput = z.infer<typeof spaceFormSchema>

export type SpaceWriteInput = {
  label?: string
  type?: SpaceType
  zone?: string
  placement?: SpacePlacement
  clearPlacement?: boolean
}

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
