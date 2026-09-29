import { apiClient } from '@/lib/api-client'
import {
  grantListResponseSchema,
  grantSchema,
  type CreateGrantInput,
  type Grant,
  type GrantListParams,
  type GrantListResponse,
} from './schemas'

function pageParams(params?: GrantListParams) {
  return { page: params?.page, per_page: params?.perPage }
}

export async function fetchGrantsByDriver(
  driverId: string,
  params?: GrantListParams,
): Promise<GrantListResponse> {
  const { data } = await apiClient.get(`/drivers/${driverId}/grants`, {
    params: pageParams(params),
  })
  return grantListResponseSchema.parse(data)
}

export async function fetchGrantsByLot(
  lotId: string,
  params?: GrantListParams,
): Promise<GrantListResponse> {
  const { data } = await apiClient.get(`/lots/${lotId}/grants`, {
    params: pageParams(params),
  })
  return grantListResponseSchema.parse(data)
}

export async function createGrant(input: CreateGrantInput): Promise<Grant> {
  const { data } = await apiClient.post('/grants', {
    driverId: input.driverId,
    lotId: input.lotId,
    validFrom: input.validFrom || undefined,
    validTo: input.validTo || undefined,
  })
  return grantSchema.parse(data)
}

export async function revokeGrant(grantId: string): Promise<Grant> {
  const { data } = await apiClient.post(`/grants/${grantId}/revoke`)
  return grantSchema.parse(data)
}
