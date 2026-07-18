import { apiClient } from '@/lib/api-client'
import {
  driverListResponseSchema,
  driverSchema,
  plateListResponseSchema,
  plateSchema,
  type Driver,
  type DriverFormInput,
  type DriverListParams,
  type DriverListResponse,
  type Plate,
  type PlateFormInput,
  type PlateListParams,
  type PlateListResponse,
} from './schemas'

export async function fetchDrivers(
  params?: DriverListParams,
): Promise<DriverListResponse> {
  const { data } = await apiClient.get('/drivers', {
    params: {
      page: params?.page,
      per_page: params?.perPage,
      status: params?.status,
    },
  })
  return driverListResponseSchema.parse(data)
}

export async function fetchDriver(id: string): Promise<Driver> {
  const { data } = await apiClient.get(`/drivers/${id}`)
  return driverSchema.parse(data)
}

export async function createDriver(input: DriverFormInput): Promise<Driver> {
  const { data } = await apiClient.post('/drivers', input)
  return driverSchema.parse(data)
}

export async function updateDriver(
  id: string,
  input: Partial<DriverFormInput>,
): Promise<Driver> {
  const { data } = await apiClient.patch(`/drivers/${id}`, input)
  return driverSchema.parse(data)
}

export async function archiveDriver(id: string): Promise<Driver> {
  const { data } = await apiClient.post(`/drivers/${id}/archive`)
  return driverSchema.parse(data)
}

export async function restoreDriver(id: string): Promise<Driver> {
  const { data } = await apiClient.post(`/drivers/${id}/restore`)
  return driverSchema.parse(data)
}

export async function fetchPlatesByDriver(
  driverId: string,
  params?: PlateListParams,
): Promise<PlateListResponse> {
  const { data } = await apiClient.get(`/drivers/${driverId}/plates`, {
    params: {
      page: params?.page,
      per_page: params?.perPage,
    },
  })
  return plateListResponseSchema.parse(data)
}

export async function addPlate(
  driverId: string,
  input: PlateFormInput,
): Promise<Plate> {
  const { data } = await apiClient.post(`/drivers/${driverId}/plates`, input)
  return plateSchema.parse(data)
}

export async function reassignPlate(
  plateId: string,
  targetDriverId: string,
): Promise<Plate> {
  const { data } = await apiClient.post(`/plates/${plateId}/reassign`, {
    targetDriverId,
  })
  return plateSchema.parse(data)
}

export async function deactivatePlate(id: string): Promise<Plate> {
  const { data } = await apiClient.post(`/plates/${id}/deactivate`)
  return plateSchema.parse(data)
}

export async function reactivatePlate(id: string): Promise<Plate> {
  const { data } = await apiClient.post(`/plates/${id}/reactivate`)
  return plateSchema.parse(data)
}
