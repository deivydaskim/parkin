import { apiClient } from '@/lib/api-client'
import {
  grantListResponseSchema,
  grantSchema,
  type Grant,
  type GrantFormInput,
  type GrantListParams,
  type GrantListResponse,
} from './schemas'

export async function fetchGrantsByDriver(
  driverId: string,
  params?: GrantListParams,
): Promise<GrantListResponse> {
  const { data } = await apiClient.get(`/drivers/${driverId}/grants`, {
    params: {
      page: params?.page,
      per_page: params?.perPage,
    },
  })
  return grantListResponseSchema.parse(data)
}

export async function createGrant(
  driverId: string,
  input: GrantFormInput,
): Promise<Grant> {
  const { data } = await apiClient.post('/grants', {
    driverId,
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
