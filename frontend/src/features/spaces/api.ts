import { apiClient } from '@/lib/api-client'
import {
  spaceListResponseSchema,
  spaceSchema,
  type Space,
  type SpaceFormInput,
  type SpaceWriteInput,
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
  input: SpaceWriteInput,
): Promise<Space> {
  const { data } = await apiClient.post(`/lots/${lotId}/spaces`, input)
  return spaceSchema.parse(data)
}

export async function updateSpace(
  id: string,
  input: SpaceWriteInput,
): Promise<Space> {
  const { data } = await apiClient.patch(`/spaces/${id}`, input)
  return spaceSchema.parse(data)
}

export async function deactivateSpace(id: string): Promise<Space> {
  const { data } = await apiClient.post(`/spaces/${id}/deactivate`)
  return spaceSchema.parse(data)
}

export async function reactivateSpace(id: string): Promise<Space> {
  const { data } = await apiClient.post(`/spaces/${id}/reactivate`)
  return spaceSchema.parse(data)
}

function blankToUndefined(value: string | undefined) {
  const trimmed = value?.trim()
  return trimmed ? trimmed : undefined
}

export function toCreateSpaceInput(values: SpaceFormInput): SpaceWriteInput {
  return {
    label: values.label,
    type: values.type,
    zone: blankToUndefined(values.zone),
    placement: values.isPlaced ? values.placement : undefined,
  }
}

export function toUpdateSpaceInput(
  values: SpaceFormInput,
  wasPlaced: boolean,
): SpaceWriteInput {
  return {
    label: values.label,
    type: values.type,
    zone: values.zone?.trim() ?? '',
    placement: values.isPlaced ? values.placement : undefined,
    clearPlacement: !values.isPlaced && wasPlaced ? true : undefined,
  }
}
