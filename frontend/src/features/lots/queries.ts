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
  archiveLot,
  createLot,
  fetchLot,
  fetchLots,
  restoreLot,
  updateLot,
} from './api'
import type { Lot, LotFormInput, LotListParams } from './schemas'

export function lotsQueryOptions(params?: LotListParams) {
  return queryOptions({
    queryKey: qk.lots.list(params),
    queryFn: () => fetchLots(params),
  })
}

export function lotQueryOptions(id: string) {
  return queryOptions({
    queryKey: qk.lots.detail(id),
    queryFn: () => fetchLot(id),
    enabled: !!id,
  })
}

export function useLots(params?: LotListParams) {
  return useQuery(lotsQueryOptions(params))
}

export function useLot(id: string) {
  return useQuery(lotQueryOptions(id))
}

export function useCreateLot() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: LotFormInput) => createLot(input),
    onSuccess: (lot: Lot) => {
      queryClient.invalidateQueries({ queryKey: qk.lots.list() })
      queryClient.setQueryData(qk.lots.detail(lot.id), lot)
      toast.success(`Lot "${lot.name}" created.`)
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not create lot.').message)
    },
  })
}

export function useUpdateLot(id: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: Partial<LotFormInput>) => updateLot(id, input),
    onSuccess: (lot: Lot) => {
      queryClient.invalidateQueries({ queryKey: qk.lots.list() })
      queryClient.setQueryData(qk.lots.detail(id), lot)
      toast.success(`Lot "${lot.name}" updated.`)
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not update lot.').message)
    },
  })
}

export function useArchiveLot(id: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => archiveLot(id),
    onSuccess: (lot: Lot) => {
      queryClient.invalidateQueries({ queryKey: qk.lots.list() })
      queryClient.setQueryData(qk.lots.detail(id), lot)
      toast.success(`Lot "${lot.name}" archived.`)
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not archive lot.').message)
    },
  })
}

export function useRestoreLot(id: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => restoreLot(id),
    onSuccess: (lot: Lot) => {
      queryClient.invalidateQueries({ queryKey: qk.lots.list() })
      queryClient.setQueryData(qk.lots.detail(id), lot)
      toast.success(`Lot "${lot.name}" restored.`)
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not restore lot.').message)
    },
  })
}
