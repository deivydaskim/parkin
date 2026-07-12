import { apiClient } from '@/lib/api-client'
import {
  lotListResponseSchema,
  lotSchema,
  type Lot,
  type LotFormInput,
  type LotListParams,
  type LotListResponse,
} from './schemas'

export async function fetchLots(
  params?: LotListParams,
): Promise<LotListResponse> {
  const { data } = await apiClient.get('/lots', {
    params: {
      page: params?.page,
      per_page: params?.perPage,
      status: params?.status,
    },
  })
  return lotListResponseSchema.parse(data)
}

export async function fetchLot(id: string): Promise<Lot> {
  const { data } = await apiClient.get(`/lots/${id}`)
  return lotSchema.parse(data)
}

// A blank address in the form means "none" — send undefined rather than "" so
// the API stores it as null instead of an empty string.
function normalizeAddress<T extends { address?: string }>(input: T) {
  return { ...input, address: input.address?.trim() || undefined }
}

export async function createLot(input: LotFormInput): Promise<Lot> {
  const { data } = await apiClient.post('/lots', normalizeAddress(input))
  return lotSchema.parse(data)
}

export async function updateLot(
  id: string,
  input: Partial<LotFormInput>,
): Promise<Lot> {
  const { data } = await apiClient.patch(`/lots/${id}`, normalizeAddress(input))
  return lotSchema.parse(data)
}

export async function archiveLot(id: string): Promise<Lot> {
  const { data } = await apiClient.post(`/lots/${id}/archive`)
  return lotSchema.parse(data)
}
