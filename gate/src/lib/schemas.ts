import { z } from 'zod'

export const directionSchema = z.enum(['Enter', 'Exit'])
export type Direction = z.infer<typeof directionSchema>

export const decisionSchema = z.enum(['Allow', 'Deny'])
export type DecisionValue = z.infer<typeof decisionSchema>

export const poolSchema = z.enum(['General', 'Reserved'])
export type Pool = z.infer<typeof poolSchema>

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
