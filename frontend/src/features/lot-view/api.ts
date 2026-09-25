import { apiClient } from '@/lib/api-client'
import {
  lotLayoutViewSchema,
  type ApplyLayoutInput,
  type LotLayoutView,
} from './schemas'

export async function fetchLotLayout(lotId: string): Promise<LotLayoutView> {
  const { data } = await apiClient.get(`/lots/${lotId}/layout`)
  return lotLayoutViewSchema.parse(data)
}

export async function applyLotLayout(
  lotId: string,
  input: ApplyLayoutInput,
): Promise<LotLayoutView> {
  const { data } = await apiClient.put(`/lots/${lotId}/layout`, input)
  return lotLayoutViewSchema.parse(data)
}
