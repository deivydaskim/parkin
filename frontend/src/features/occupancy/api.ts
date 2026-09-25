import { apiClient } from '@/lib/api-client'
import {
  lotOccupancyListSchema,
  lotOccupancySchema,
  type LotOccupancy,
} from './schemas'

export async function fetchLotOccupancy(lotId: string): Promise<LotOccupancy> {
  const { data } = await apiClient.get(`/lots/${lotId}/occupancy`)
  return lotOccupancySchema.parse(data)
}

export async function fetchAllLotOccupancy(): Promise<LotOccupancy[]> {
  const { data } = await apiClient.get('/occupancy')
  return lotOccupancyListSchema.parse(data)
}
