import { GrantForm } from '@/features/grants/components/GrantForm'
import { GrantList } from '@/features/grants/components/GrantList'
import { useCreateGrant, useGrants, useRevokeGrant } from '@/features/grants/queries'

type Props = {
  driverId: string
}

export function DriverGrantsPanel({ driverId }: Props) {
  const { data: grantsData, isLoading } = useGrants(driverId)
  const createGrantMutation = useCreateGrant(driverId)
  const revokeGrantMutation = useRevokeGrant(driverId)

  return (
    <div className="space-y-4">
      <GrantForm
        onSubmit={(values) => createGrantMutation.mutate(values)}
        isSubmitting={createGrantMutation.isPending}
      />

      {isLoading ? (
        <p className="text-sm text-muted-foreground">Loading…</p>
      ) : (
        <GrantList
          grants={grantsData?.items ?? []}
          onRevoke={(grantId) => revokeGrantMutation.mutate(grantId)}
          isRevoking={revokeGrantMutation.isPending}
        />
      )}
    </div>
  )
}
