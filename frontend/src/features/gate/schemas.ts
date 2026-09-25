import { z } from 'zod'

export const directionSchema = z.enum(['Enter', 'Exit'])
export type Direction = z.infer<typeof directionSchema>
export const Direction = directionSchema.enum

export const decisionSchema = z.enum(['Allow', 'Deny'])
export type DecisionValue = z.infer<typeof decisionSchema>
export const Decision = decisionSchema.enum

export const poolSchema = z.enum(['General', 'Reserved'])
export type Pool = z.infer<typeof poolSchema>

export const eventSourceSchema = z.enum(['Lpr', 'Manual'])
export type EventSource = z.infer<typeof eventSourceSchema>

export const denyReasonSchema = z.enum([
  'NotAuthorized',
  'LotFull',
  'NoOpenSession',
  'LotArchived',
  'LotNotFound',
])
export type DenyReason = z.infer<typeof denyReasonSchema>

export const accessEventDecisionSchema = z.object({
  eventId: z.uuid().nullable(),
  decision: decisionSchema,
  reason: denyReasonSchema.nullable(),
  pool: poolSchema.nullable(),
  reservedSpaceLabel: z.string().nullable(),
  sessionId: z.uuid().nullable(),
  occurredAt: z.iso.datetime({ offset: true }),
})

export type AccessEventDecision = z.infer<typeof accessEventDecisionSchema>

export const manualEventFormSchema = z.object({
  plate: z
    .string()
    .trim()
    .min(1, 'Plate is required')
    .max(20, 'Plate must be 20 characters or fewer'),
  direction: directionSchema,
})

export type ManualEventFormInput = z.infer<typeof manualEventFormSchema>

export const accessEventSchema = z.object({
  id: z.uuid(),
  plate: z.string(),
  direction: directionSchema,
  decision: decisionSchema,
  reason: denyReasonSchema.nullable(),
  pool: poolSchema.nullable(),
  reservedSpaceLabel: z.string().nullable(),
  source: eventSourceSchema,
  driverId: z.uuid().nullable(),
  driverName: z.string().nullable(),
  occurredAt: z.iso.datetime({ offset: true }),
})

export type AccessEvent = z.infer<typeof accessEventSchema>

export type AccessEventListParams = {
  page?: number
  perPage?: number
}

export const accessEventListResponseSchema = z.object({
  items: z.array(accessEventSchema),
  page: z.number(),
  perPage: z.number(),
  totalCount: z.number(),
  totalPages: z.number(),
})

export type AccessEventListResponse = z.infer<
  typeof accessEventListResponseSchema
>
