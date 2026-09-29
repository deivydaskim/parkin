import { apiClient } from '@/lib/api-client'
import {
  activeSessionListResponseSchema,
  type ActiveSessionListParams,
  type ActiveSessionListResponse,
} from './schemas'

export async function fetchActiveSessionsByLot(
  lotId: string,
  params?: ActiveSessionListParams,
): Promise<ActiveSessionListResponse> {
  const { data } = await apiClient.get(`/lots/${lotId}/active-sessions`, {
    params: { page: params?.page, per_page: params?.perPage },
  })
  return activeSessionListResponseSchema.parse(data)
}
