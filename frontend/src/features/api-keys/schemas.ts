import { z } from 'zod'

export const apiKeyStatusSchema = z.enum(['Active', 'Revoked'])
export type ApiKeyStatus = z.infer<typeof apiKeyStatusSchema>

// Full resource, as returned by the API. Never carries the raw key or its hash.
export const apiKeySchema = z.object({
  id: z.uuid(),
  name: z.string(),
  prefix: z.string(),
  status: apiKeyStatusSchema,
  createdAt: z.string(),
  revokedAt: z.string().nullable(),
})

export type ApiKey = z.infer<typeof apiKeySchema>

export const createApiKeySchema = z.object({
  name: z
    .string()
    .min(1, 'Name is required')
    .max(200, 'Name must not exceed 200 characters'),
})

export type CreateApiKeyInput = z.infer<typeof createApiKeySchema>

// Only the create response ever carries the raw key, and only this once.
export const createApiKeyResponseSchema = apiKeySchema.extend({
  key: z.string(),
})

export type CreateApiKeyResponse = z.infer<typeof createApiKeyResponseSchema>

export const apiKeyListResponseSchema = z.array(apiKeySchema)
