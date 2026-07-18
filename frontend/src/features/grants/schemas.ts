import { z } from 'zod'

export const grantStatusSchema = z.enum(['Active', 'Revoked'])
export type GrantStatus = z.infer<typeof grantStatusSchema>
export const GrantStatus = grantStatusSchema.enum

// Full resource, as returned by the API.
export const grantSchema = z.object({
  id: z.uuid(),
  driverId: z.uuid(),
  parkingLotId: z.uuid(),
  validFrom: z.iso.datetime({ offset: true }),
  validTo: z.iso.datetime({ offset: true }).nullable(),
  status: grantStatusSchema,
})

export type Grant = z.infer<typeof grantSchema>

export const grantFormSchema = z.object({
  lotId: z.uuid('Lot is required'),
  validFrom: z.string().optional(),
  validTo: z.string().optional(),
})

export type GrantFormInput = z.infer<typeof grantFormSchema>

export const grantListParamsSchema = z.object({
  page: z.number().int().min(1).optional(),
  perPage: z.number().int().min(1).max(100).optional(),
})

export type GrantListParams = z.infer<typeof grantListParamsSchema>

export const grantListResponseSchema = z.object({
  items: z.array(grantSchema),
  page: z.number(),
  perPage: z.number(),
  totalCount: z.number(),
  totalPages: z.number(),
})

export type GrantListResponse = z.infer<typeof grantListResponseSchema>
