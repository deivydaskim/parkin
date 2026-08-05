import { queryOptions, useQuery } from '@tanstack/react-query'
import { qk } from '@/lib/query-keys'
import { fetchAuditLog } from './api'
import type { AuditListParams } from './schemas'

export function auditLogQueryOptions(params?: AuditListParams) {
  return queryOptions({
    queryKey: qk.audit.list(params),
    queryFn: () => fetchAuditLog(params),
  })
}

export function useAuditLog(params?: AuditListParams) {
  return useQuery(auditLogQueryOptions(params))
}
