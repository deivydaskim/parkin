import {
  queryOptions,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import { toast } from 'sonner'
import { qk } from '@/lib/query-keys'
import { parseApiError } from '@/lib/api-error'
import {
  addPlate,
  archiveDriver,
  createDriver,
  deactivatePlate,
  fetchDriver,
  fetchDrivers,
  fetchPlatesByDriver,
  reassignPlate,
  updateDriver,
} from './api'
import type {
  Driver,
  DriverFormInput,
  DriverListParams,
  Plate,
  PlateFormInput,
  PlateListParams,
} from './schemas'

export function driversQueryOptions(params?: DriverListParams) {
  return queryOptions({
    queryKey: qk.drivers.list(params),
    queryFn: () => fetchDrivers(params),
  })
}

export function driverQueryOptions(id: string) {
  return queryOptions({
    queryKey: qk.drivers.detail(id),
    queryFn: () => fetchDriver(id),
    enabled: !!id,
  })
}

export function platesQueryOptions(driverId: string, params?: PlateListParams) {
  return queryOptions({
    queryKey: qk.plates.list(driverId, params),
    queryFn: () => fetchPlatesByDriver(driverId, params),
    enabled: !!driverId,
  })
}

export function useDrivers(params?: DriverListParams) {
  return useQuery(driversQueryOptions(params))
}

export function useDriver(id: string) {
  return useQuery(driverQueryOptions(id))
}

export function usePlates(driverId: string, params?: PlateListParams) {
  return useQuery(platesQueryOptions(driverId, params))
}

export function useCreateDriver() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: DriverFormInput) => createDriver(input),
    onSuccess: (driver: Driver) => {
      queryClient.invalidateQueries({ queryKey: qk.drivers.list() })
      queryClient.setQueryData(qk.drivers.detail(driver.id), driver)
      toast.success(`Driver "${driver.name}" created.`)
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not create driver.').message)
    },
  })
}

export function useUpdateDriver(id: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: Partial<DriverFormInput>) => updateDriver(id, input),
    onSuccess: (driver: Driver) => {
      queryClient.invalidateQueries({ queryKey: qk.drivers.list() })
      queryClient.setQueryData(qk.drivers.detail(id), driver)
      toast.success(`Driver "${driver.name}" updated.`)
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not update driver.').message)
    },
  })
}

export function useArchiveDriver(id: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => archiveDriver(id),
    onSuccess: (driver: Driver) => {
      queryClient.invalidateQueries({ queryKey: qk.drivers.list() })
      queryClient.setQueryData(qk.drivers.detail(id), driver)
      toast.success(`Driver "${driver.name}" archived.`)
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not archive driver.').message)
    },
  })
}

export function useAddPlate(driverId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: PlateFormInput) => addPlate(driverId, input),
    onSuccess: (plate: Plate) => {
      queryClient.invalidateQueries({ queryKey: qk.plates.list(driverId) })
      queryClient.invalidateQueries({ queryKey: qk.drivers.detail(driverId) })
      queryClient.invalidateQueries({ queryKey: qk.drivers.list() })
      toast.success(`Plate "${plate.normalizedPlateNumber}" added.`)
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not add plate.').message)
    },
  })
}

export function useReassignPlate(driverId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (variables: { plateId: string; targetDriverId: string }) =>
      reassignPlate(variables.plateId, variables.targetDriverId),
    onSuccess: (plate: Plate) => {
      queryClient.invalidateQueries({ queryKey: qk.plates.list(driverId) })
      queryClient.invalidateQueries({
        queryKey: qk.plates.list(plate.driverId),
      })
      queryClient.invalidateQueries({ queryKey: qk.drivers.detail(driverId) })
      queryClient.invalidateQueries({
        queryKey: qk.drivers.detail(plate.driverId),
      })
      queryClient.invalidateQueries({ queryKey: qk.drivers.list() })
      toast.success(`Plate "${plate.normalizedPlateNumber}" reassigned.`)
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not reassign plate.').message)
    },
  })
}

export function useDeactivatePlate(driverId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (plateId: string) => deactivatePlate(plateId),
    onSuccess: (plate: Plate) => {
      queryClient.invalidateQueries({ queryKey: qk.plates.list(driverId) })
      queryClient.invalidateQueries({ queryKey: qk.drivers.detail(driverId) })
      toast.success(`Plate "${plate.normalizedPlateNumber}" deactivated.`)
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not deactivate plate.').message)
    },
  })
}
