import { z } from 'zod'
import { lotLayoutSchema, lotStatusSchema } from '@/features/lots/schemas'
import {
  spacePlacementSchema,
  spaceStatusSchema,
  spaceTypeSchema,
  type SpacePlacement,
} from '@/features/spaces/schemas'

export const layoutReservationSchema = z.object({
  id: z.uuid(),
  driverId: z.uuid(),
  driverName: z.string(),
})

export const layoutSpaceSchema = z.object({
  id: z.uuid(),
  label: z.string(),
  type: spaceTypeSchema,
  status: spaceStatusSchema,
  zone: z.string().nullable(),
  placement: spacePlacementSchema.nullable(),
  reservation: layoutReservationSchema.nullable(),
})

export type LayoutSpace = z.infer<typeof layoutSpaceSchema>

export const lotLayoutViewSchema = z.object({
  lot: z.object({
    id: z.uuid(),
    name: z.string(),
    status: lotStatusSchema,
    layout: lotLayoutSchema.nullable(),
  }),
  spaces: z.array(layoutSpaceSchema),
})

export type LotLayoutView = z.infer<typeof lotLayoutViewSchema>

export type SpaceLayoutChange = {
  spaceId: string
  placement: SpacePlacement | null
  zone?: string
}

export type ApplyLayoutInput = {
  layout?: { widthMeters: number; lengthMeters: number; levelCount: number }
  clearLayout?: boolean
  spaces: SpaceLayoutChange[]
}

export type BayStyle =
  'general' | 'reservedAssigned' | 'reservedUnassigned' | 'inactive'

export function bayStyleOf(space: LayoutSpace): BayStyle {
  if (space.status === 'Inactive') return 'inactive'
  if (space.type === 'General') return 'general'
  return space.reservation ? 'reservedAssigned' : 'reservedUnassigned'
}
