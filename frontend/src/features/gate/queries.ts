import { useRef } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { qk } from '@/lib/query-keys'
import { parseApiError } from '@/lib/api-error'
import { recordManualEvent } from './api'
import type { ManualEventFormInput } from './schemas'

type PendingAttempt = {
  signature: string
  idempotencyKey: string
}

export function useManualEvent(lotId: string) {
  const queryClient = useQueryClient()
  const pendingAttempt = useRef<PendingAttempt | null>(null)

  function idempotencyKeyFor(input: ManualEventFormInput) {
    const signature = JSON.stringify(input)
    if (pendingAttempt.current?.signature !== signature) {
      pendingAttempt.current = { signature, idempotencyKey: crypto.randomUUID() }
    }
    return pendingAttempt.current.idempotencyKey
  }

  return useMutation({
    mutationFn: (input: ManualEventFormInput) =>
      recordManualEvent(lotId, input, idempotencyKeyFor(input)),
    onSuccess: () => {
      pendingAttempt.current = null
      queryClient.invalidateQueries({ queryKey: qk.occupancy.lot(lotId) })
    },
    onError: (error) => {
      toast.error(parseApiError(error, 'Could not record the event.').message)
    },
  })
}
