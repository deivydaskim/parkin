import { apiClient } from '@/lib/api-client'
import {
  spaceListResponseSchema,
  spaceSchema,
  type Space,
  type SpaceFormInput,
  type SpaceListParams,
  type SpaceListResponse,
} from './schemas'

export async function fetchSpaces(
  lotId: string,
  params?: SpaceListParams,
): Promise<SpaceListResponse> {
  const { data } = await apiClient.get(`/lots/${lotId}/spaces`, {
    params: {
      page: params?.page,
      per_page: params?.perPage,
      status: params?.status,
    },
  })
  return spaceListResponseSchema.parse(data)
}

export async function createSpace(
  lotId: string,
  input: SpaceFormInput,
): Promise<Space> {
  const { data } = await apiClient.post(`/lots/${lotId}/spaces`, input)
  return spaceSchema.parse(data)
}

export async function updateSpace(
  id: string,
  input: Partial<SpaceFormInput>,
): Promise<Space> {
  const { data } = await apiClient.patch(`/spaces/${id}`, input)
  return spaceSchema.parse(data)
}

export async function deactivateSpace(id: string): Promise<Space> {
  const { data } = await apiClient.post(`/spaces/${id}/deactivate`)
  return spaceSchema.parse(data)
}
