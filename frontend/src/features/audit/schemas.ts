import { z } from 'zod'

export const auditActorTypeSchema = z.enum(['Staff', 'System', 'Api'])
export type AuditActorType = z.infer<typeof auditActorTypeSchema>

export const auditEntityTypeSchema = z.enum([
  'User',
  'ParkingLot',
  'ParkingSpace',
  'Driver',
  'Plate',
  'AccessGrant',
  'ApiKey',
  'Reservation',
])
export type AuditEntityType = z.infer<typeof auditEntityTypeSchema>

// Known entity-type vocabulary, for the filter Select.
export const auditEntityTypes = auditEntityTypeSchema.options

// Full resource, as returned by the API.
export const auditLogEntrySchema = z.object({
  id: z.uuid(),
  actorType: auditActorTypeSchema,
  actorId: z.uuid().nullable(),
  action: z.string(),
  entityType: z.string(),
  entityId: z.uuid(),
  occurredAt: z.iso.datetime({ offset: true }),
  metadataJson: z.string().nullable(),
})

export type AuditLogEntry = z.infer<typeof auditLogEntrySchema>

// Filter-bar shape, reused as both the form resolver and the query-param shape
// sent to the API (empty strings mean "no filter" and are dropped before the request).
export const auditFiltersSchema = z.object({
  from: z.string().optional(),
  to: z.string().optional(),
  actor: z.string().optional(),
  actorType: z.union([auditActorTypeSchema, z.literal('')]).optional(),
  entity: z.union([auditEntityTypeSchema, z.literal('')]).optional(),
})

export type AuditFilters = z.infer<typeof auditFiltersSchema>

export const auditListParamsSchema = auditFiltersSchema.extend({
  page: z.number().int().min(1).optional(),
  perPage: z.number().int().min(1).max(100).optional(),
})

export type AuditListParams = z.infer<typeof auditListParamsSchema>

export const auditListResponseSchema = z.object({
  items: z.array(auditLogEntrySchema),
  page: z.number(),
  perPage: z.number(),
  totalCount: z.number(),
  totalPages: z.number(),
})

export type AuditListResponse = z.infer<typeof auditListResponseSchema>
