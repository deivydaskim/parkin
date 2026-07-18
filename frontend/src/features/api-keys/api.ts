import { apiClient } from '@/lib/api-client'
import {
  apiKeyListResponseSchema,
  createApiKeyResponseSchema,
  type ApiKey,
  type CreateApiKeyInput,
  type CreateApiKeyResponse,
} from './schemas'

export async function fetchApiKeys(): Promise<ApiKey[]> {
  const { data } = await apiClient.get('/api-keys')
  return apiKeyListResponseSchema.parse(data)
}

export async function createApiKey(
  input: CreateApiKeyInput,
): Promise<CreateApiKeyResponse> {
  const { data } = await apiClient.post('/api-keys', input)
  return createApiKeyResponseSchema.parse(data)
}

export async function revokeApiKey(id: string): Promise<void> {
  await apiClient.post(`/api-keys/${id}/revoke`, {})
}
