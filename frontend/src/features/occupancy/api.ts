import { apiClient } from '@/lib/api-client'
import { lotOccupancySchema, type LotOccupancy } from './schemas'

export async function fetchLotOccupancy(lotId: string): Promise<LotOccupancy> {
  const { data } = await apiClient.get(`/lots/${lotId}/occupancy`)
  return lotOccupancySchema.parse(data)
}
