import { apiClient } from '@/lib/api-client'
import {
  accessEventDecisionSchema,
  accessEventListResponseSchema,
  type AccessEventDecision,
  type AccessEventListParams,
  type AccessEventListResponse,
  type ManualEventFormInput,
} from './schemas'

export async function recordManualEvent(
  lotId: string,
  input: ManualEventFormInput,
  idempotencyKey: string,
): Promise<AccessEventDecision> {
  const { data } = await apiClient.post(`/lots/${lotId}/manual-events`, input, {
    headers: { 'Idempotency-Key': idempotencyKey },
  })
  return accessEventDecisionSchema.parse(data)
}

export async function fetchAccessEvents(
  lotId: string,
  params?: AccessEventListParams,
): Promise<AccessEventListResponse> {
  const { data } = await apiClient.get(`/lots/${lotId}/access-events`, {
    params: { page: params?.page, per_page: params?.perPage },
  })
  return accessEventListResponseSchema.parse(data)
}
