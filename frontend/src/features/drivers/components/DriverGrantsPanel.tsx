import { useState } from 'react'
import { KeyRound, Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { DataTablePagination } from '@/components/DataTablePagination'
import { EmptyState } from '@/components/EmptyState'
import { ListSkeleton } from '@/components/Skeletons'
import { RoleGate } from '@/features/auth/components/RoleGate'
import { GrantForm } from '@/features/grants/components/GrantForm'
import { GrantList } from '@/features/grants/components/GrantList'
import { useCreateGrant, useGrants, useRevokeGrant } from '@/features/grants/queries'
import type { GrantFormInput } from '@/features/grants/schemas'

const PAGE_SIZE = 20

type Props = {
  driverId: string
  canEdit: boolean
}

export function DriverGrantsPanel({ driverId, canEdit }: Props) {
  const [page, setPage] = useState(1)
  const [isGrantOpen, setIsGrantOpen] = useState(false)
  const { data: grantsData, isLoading } = useGrants(driverId, {
    page,
    perPage: PAGE_SIZE,
  })
  const createGrantMutation = useCreateGrant(driverId)
  const revokeGrantMutation = useRevokeGrant(driverId)
  const grants = grantsData?.items ?? []

  function handleCreate(values: GrantFormInput) {
    createGrantMutation.mutate(values, {
      onSuccess: () => setIsGrantOpen(false),
    })
  }

  const grantButton = canEdit ? (
    <RoleGate roles={['Operator', 'SystemAdmin']}>
      <Button onClick={() => setIsGrantOpen(true)}>
        <Plus />
        Grant access
      </Button>
    </RoleGate>
  ) : null

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <p className="text-sm text-muted-foreground">
          Grants decide which restricted lots this driver may enter. Open lots
          admit everyone.
        </p>
        {grants.length > 0 ? grantButton : null}
      </div>

      {isLoading ? (
        <ListSkeleton rows={2} />
      ) : grants.length === 0 ? (
        <EmptyState
          icon={KeyRound}
          title="No lot access yet"
          hint="This driver can only use open lots until you grant access to a restricted one."
          action={grantButton}
        />
      ) : (
        <GrantList
          grants={grants}
          canEdit={canEdit}
          isRevoking={revokeGrantMutation.isPending}
          onRevoke={(grantId, onDone) =>
            revokeGrantMutation.mutate(grantId, { onSuccess: onDone })
          }
        />
      )}

      {grantsData && grantsData.totalPages > 1 ? (
        <DataTablePagination
          page={page}
          totalPages={grantsData.totalPages}
          totalCount={grantsData.totalCount}
          perPage={PAGE_SIZE}
          itemLabel="grants"
          onPageChange={setPage}
        />
      ) : null}

      <Dialog open={isGrantOpen} onOpenChange={setIsGrantOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Grant lot access</DialogTitle>
            <DialogDescription>
              Let this driver enter a restricted lot, optionally for a limited
              period.
            </DialogDescription>
          </DialogHeader>
          {isGrantOpen ? (
            <GrantForm
              onSubmit={handleCreate}
              isSubmitting={createGrantMutation.isPending}
            />
          ) : null}
        </DialogContent>
      </Dialog>
    </div>
  )
}
