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
  createSpace,
  deactivateSpace,
  fetchSpaces,
  reactivateSpace,
  updateSpace,
} from './api'
import type { Space, SpaceFormInput, SpaceListParams } from './schemas'

export function spacesQueryOptions(lotId: string, params?: SpaceListParams) {
  return queryOptions({
    queryKey: qk.spaces.list(lotId, params),
    queryFn: () => fetchSpaces(lotId, params),
    enabled: !!lotId,
  })
}

export function useSpaces(lotId: string, params?: SpaceListParams) {
  return useQuery(spacesQueryOptions(lotId, params))
}

export function useCreateSpace(lotId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: SpaceFormInput) => createSpace(lotId, input),
    onSuccess: (space: Space) => {
      queryClient.invalidateQueries({ queryKey: qk.spaces.list(lotId) })
      queryClient.invalidateQueries({ queryKey: qk.lots.detail(lotId) })
      toast.success(`Space "${space.label}" created.`)
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not create space.').message)
    },
  })
}

export function useUpdateSpace(id: string, lotId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: Partial<SpaceFormInput>) => updateSpace(id, input),
    onSuccess: (space: Space) => {
      queryClient.invalidateQueries({ queryKey: qk.spaces.list(lotId) })
      queryClient.invalidateQueries({ queryKey: qk.lots.detail(lotId) })
      toast.success(`Space "${space.label}" updated.`)
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not update space.').message)
    },
  })
}

export function useDeactivateSpace(id: string, lotId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => deactivateSpace(id),
    onSuccess: (space: Space) => {
      queryClient.invalidateQueries({ queryKey: qk.spaces.list(lotId) })
      queryClient.invalidateQueries({ queryKey: qk.lots.detail(lotId) })
      toast.success(`Space "${space.label}" deactivated.`)
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not deactivate space.').message)
    },
  })
}

export function useReactivateSpace(id: string, lotId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => reactivateSpace(id),
    onSuccess: (space: Space) => {
      queryClient.invalidateQueries({ queryKey: qk.spaces.list(lotId) })
      queryClient.invalidateQueries({ queryKey: qk.lots.detail(lotId) })
      toast.success(`Space "${space.label}" reactivated.`)
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not reactivate space.').message)
    },
  })
}
