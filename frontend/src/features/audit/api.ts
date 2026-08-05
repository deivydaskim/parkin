import { apiClient } from '@/lib/api-client'
import { auditListResponseSchema, type AuditListParams, type AuditListResponse } from './schemas'

export async function fetchAuditLog(
  params?: AuditListParams,
): Promise<AuditListResponse> {
  const { data } = await apiClient.get('/audit', {
    params: {
      page: params?.page,
      per_page: params?.perPage,
      from: params?.from || undefined,
      to: params?.to || undefined,
      actor: params?.actor || undefined,
      actor_type: params?.actorType || undefined,
      entity: params?.entity || undefined,
    },
  })
  return auditListResponseSchema.parse(data)
}
