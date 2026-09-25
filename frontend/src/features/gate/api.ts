import { apiClient } from '@/lib/api-client'
import {
  accessEventDecisionSchema,
  type AccessEventDecision,
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
