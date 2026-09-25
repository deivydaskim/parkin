import { z } from 'zod'

export const directionSchema = z.enum(['Enter', 'Exit'])
export type Direction = z.infer<typeof directionSchema>
export const Direction = directionSchema.enum

export const decisionSchema = z.enum(['Allow', 'Deny'])
export const Decision = decisionSchema.enum

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
  pool: z.enum(['General', 'Reserved']).nullable(),
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
