import {
  queryOptions,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import { toast } from 'sonner'
import { qk } from '@/lib/query-keys'
import { parseApiError } from '@/lib/api-error'
import { invalidateLotSpaceViews } from '@/features/spaces/queries'
import { applyLotLayout, fetchLotLayout } from './api'
import type { ApplyLayoutInput, LotLayoutView } from './schemas'

export function lotLayoutQueryOptions(lotId: string) {
  return queryOptions({
    queryKey: qk.lotLayout.detail(lotId),
    queryFn: () => fetchLotLayout(lotId),
    enabled: !!lotId,
  })
}

export function useLotLayout(lotId: string) {
  return useQuery(lotLayoutQueryOptions(lotId))
}

export function useApplyLotLayout(lotId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: ApplyLayoutInput) => applyLotLayout(lotId, input),
    onSuccess: (view: LotLayoutView, input) => {
      queryClient.setQueryData(qk.lotLayout.detail(lotId), view)
      invalidateLotSpaceViews(queryClient, lotId)
      toast.success(`Layout saved — ${input.spaces.length} space(s) updated.`)
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not save the layout.').message)
    },
  })
}
